/**
 * @file Manages all client-side logic for the Videos admin page.
 * This includes tab navigation, CRUD operations for video metadata, file uploads,
 * and an HLS video player.
 */

// --- Global Modal Elements & Functions ---
const editModal = document.getElementById('edit-modal');
const editForm = document.getElementById('edit-video-form');
const editVideoIdInput = document.getElementById('edit-video-id');
const editVideoTitleInput = document.getElementById('edit-video-title');
const editVideoDescriptionInput = document.getElementById('edit-video-description');
const editThumbnailInput = document.getElementById('edit-video-thumbnail-file');
const editThumbnailPreview = document.getElementById('edit-thumbnail-preview');

/**
 * Opens the edit modal and populates it with the selected video's data.
 * @param {object} video - The video object to be edited.
 */
function openEditModal(video) {
    editVideoIdInput.value = video.id;
    editVideoTitleInput.value = video.title;
    editVideoDescriptionInput.value = video.description;

    if (video.thumbnailUrl) {
        editThumbnailPreview.src = video.thumbnailUrl;
        editThumbnailPreview.style.display = 'block';
    } else {
        editThumbnailPreview.style.display = 'none';
    }
    editModal.style.display = 'block';
}

/**
 * Closes the video edit modal.
 */
function closeEditModal() {
    editModal.style.display = 'none';
}

// Close modal if user clicks outside of it.
window.onclick = function (event) {
    if (event.target == editModal) {
        closeEditModal();
    }
};

/**
 * Prompts the user for confirmation and then deletes a video and its associated files.
 * @param {string} videoId - The ID of the video to delete.
 */
async function deleteVideo(videoId) {
    const result = await Swal.fire({
        title: 'Are you sure?',
        text: "This action cannot be undone.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    });

    if (!result.isConfirmed) {
        return;
    }

    try {
        const response = await fetch(`/api/admin/videos/${videoId}`, { method: 'DELETE' });
        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Failed to delete the video.');
        }

        const successData = await response.json();

        Swal.fire(
            'Deleted!',
            successData.message,
            'success'
        );

        document.dispatchEvent(new CustomEvent('reloadAllVideos'));
    } catch (error) {
        console.error('Error deleting video:', error);
        Swal.fire(
            'Error!',
            error.message,
            'error'
        );
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // --- DOM Element Selections ---
    const createForm = document.getElementById('create-video-form');
    const fileInput = document.getElementById('video-file-input');
    const uploadStatusDiv = document.getElementById('upload-status');
    const metadataFieldset = document.getElementById('metadata-fieldset');
    const storageIdentifierInput = document.getElementById('storage-identifier-input');
    const saveButton = document.getElementById('save-video-button');
    const titleInput = document.getElementById('video-title');
    const descriptionInput = document.getElementById('video-description');
    const courseNameInput = document.getElementById('course-name');
    const createThumbnailInput = document.getElementById('video-thumbnail-file');
    const createThumbnailPreview = document.getElementById('create-thumbnail-preview');
    const courseSelect = document.getElementById('video-course-select');
    const newCourseInput = document.getElementById('video-course-new-name');

    const navCrud = document.getElementById('nav-crud');
    const navViewer = document.getElementById('nav-viewer');
    const panelCrud = document.getElementById('panel-crud');
    const panelViewer = document.getElementById('panel-viewer');

    const videosTableBody = document.getElementById('videos-table-body');
    const viewerPlaylist = document.getElementById('viewer-playlist');
    const videoPlayer = document.getElementById('video-player');
    let hlsInstance;

    // --- State Management for Caching and Infinite Scroll ---
    let crudState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };
    let viewerState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };

    // --- Rendering Functions ---

    /**
     * Renders a single video row in the CRUD management table.
     * Attaches event listeners for edit and delete buttons directly to the new row.
     * @param {object} video - The video object to render.
     */
    function renderCrudTableRow(video) {
        const row = document.createElement('tr');
        row.innerHTML = `
        <td>${video.title}</td>
        <td>${video.courseName}</td>
        <td><span class="status-badge status-${video.status.toLowerCase()}">${video.status}</span></td>
        <td>${new Date(video.uploadDate).toLocaleDateString()}</td>
        <td class="actions">
            <button class="btn btn-secondary btn-sm btn-edit">Editar</button>
            <button class="btn btn-danger btn-sm btn-delete">Deletar</button>
        </td>
    `;

        row.querySelector('.btn-edit').addEventListener('click', () => {
            openEditModal(video);
        });

        row.querySelector('.btn-delete').addEventListener('click', () => {
            deleteVideo(video.id);
        });

        videosTableBody.appendChild(row);
    }

    /**
     * Renders a single video item in the viewer playlist.
     * @param {object} video - The video object to render.
     */
    function renderViewerPlaylistItem(video) {
        const item = document.createElement('div');
        item.className = 'playlist-item';
        item.innerHTML = `<h4>${video.title}</h4><p>${video.courseName}</p>`;
        item.addEventListener('click', () => playVideo(video.storageIdentifier, item));
        viewerPlaylist.appendChild(item);
    }

    /**
     * A generic function to fetch paginated data, with caching and infinite scroll support.
     * @param {object} state - The state object for the current panel (CRUD or Viewer).
     * @param {function} renderFunction - The function to call to render each item.
     * @param {HTMLElement} containerElement - The container element where items will be appended.
     */
    async function loadData(state, renderFunction, containerElement) {
        if (state.isLoading || state.allDataLoaded) return;
        state.isLoading = true;
        if (state.cache[state.currentPage]) {
            state.cache[state.currentPage].forEach(renderFunction);
            state.isLoading = false;
            return;
        }
        try {
            const response = await fetch(`/api/admin/videos?page=${state.currentPage}&pageSize=15`);
            if (!response.ok) throw new Error('Error fetching videos.');
            const paginatedResult = await response.json();
            const videos = paginatedResult.items;

            state.cache[state.currentPage] = videos; // Cache the items for the current page

            if (!videos || videos.length < 15) {
                state.allDataLoaded = true;
            }

            if (state.currentPage === 1 && (!videos || videos.length === 0)) {
                containerElement.innerHTML = (containerElement.tagName === 'TBODY')
                    ? '<tr><td colspan="5" style="text-align:center;">No videos found.</td></tr>'
                    : '<p style="padding:1rem;">No videos available.</p>';
            } else {
                videos.forEach(renderFunction);
            }
            state.currentPage++;
        } catch (error) {
            console.error('Error loading data:', error);
        } finally {
            state.isLoading = false;
        }
    }

    /**
     * Initializes or updates the HLS player to play the selected video.
     * @param {string} storageIdentifier - The unique identifier of the video to play.
     * @param {HTMLElement} clickedItem - The playlist item element that was clicked.
     */
    function playVideo(storageIdentifier, clickedItem) {
        document.querySelectorAll('.playlist-item.playing').forEach(el => el.classList.remove('playing'));
        clickedItem.classList.add('playing');
        const manifestUrl = `/api/videos/${storageIdentifier}/manifest.m3u8`;
        if (hlsInstance) hlsInstance.destroy();
        if (Hls.isSupported()) {
            hlsInstance = new Hls();
            hlsInstance.loadSource(manifestUrl);
            hlsInstance.attachMedia(videoPlayer);
            hlsInstance.on(Hls.Events.MANIFEST_PARSED, () => videoPlayer.play());
        } else if (videoPlayer.canPlayType('application/vnd.apple.mpegurl')) {
            videoPlayer.src = manifestUrl;
            videoPlayer.play();
        }
    }

    // --- Event Listeners and Initialization ---

    /**
     * Resets the state and reloads data for the CRUD panel.
     */
    function resetAndLoadCrud() {
        crudState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };
        videosTableBody.innerHTML = '';
        loadData(crudState, renderCrudTableRow, videosTableBody);
    }

    /**
     * Resets the state and reloads data for the Viewer panel.
     */
    function resetAndLoadViewer() {
        viewerState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };
        viewerPlaylist.innerHTML = '';
        loadData(viewerState, renderViewerPlaylistItem, viewerPlaylist);
    }

    // Tab navigation logic
    navCrud.addEventListener('click', () => {
        navCrud.classList.add('active');
        navViewer.classList.remove('active');
        panelCrud.classList.add('active');
        panelViewer.classList.remove('active');
        if (hlsInstance) hlsInstance.destroy();
        if (videosTableBody.innerHTML === '') resetAndLoadCrud();
    });

    navViewer.addEventListener('click', () => {
        navViewer.classList.add('active');
        navCrud.classList.remove('active');
        panelViewer.classList.add('active');
        panelCrud.classList.remove('active');
        if (viewerPlaylist.innerHTML === '') resetAndLoadViewer();
    });

    // Custom event to allow other parts of the app to trigger a data reload.
    document.addEventListener('reloadAllVideos', () => {
        resetAndLoadCrud();
        resetAndLoadViewer();
    });

    // Infinite scroll for the viewer playlist.
    viewerPlaylist.addEventListener('scroll', () => {
        if (viewerPlaylist.scrollTop + viewerPlaylist.clientHeight >= viewerPlaylist.scrollHeight - 50) {
            loadData(viewerState, renderViewerPlaylistItem, viewerPlaylist);
        }
    });

    // Infinite scroll for the CRUD table.
    window.addEventListener('scroll', () => {
        if (!panelCrud.classList.contains('active')) return;
        if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 200) {
            loadData(crudState, renderCrudTableRow, videosTableBody);
        }
    });

    // --- Video Creation Form Logic ---

    // Handles the file input change event to start the upload process.
    fileInput.addEventListener('change', async function (event) {
        const file = event.target.files[0];
        if (!file) return;

        metadataFieldset.disabled = true;
        saveButton.disabled = true;

        Swal.fire({
            title: 'Uploading...',
            text: 'Please wait while the file is being uploaded.',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        const formData = new FormData();
        formData.append('videoFile', file);

        try {
            const response = await fetch('/api/admin/videos/upload', { method: 'POST', body: formData });
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'An error occurred during upload.');
            }
            const result = await response.json();

            Swal.close();
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'success',
                title: 'Upload complete!',
                showConfirmButton: false,
                timer: 3000
            });

            uploadStatusDiv.innerHTML = `<p style="color: #198754;"><strong>Upload complete!</strong> Now, please fill in the details below.</p>`;
            storageIdentifierInput.value = result.storageIdentifier;
            metadataFieldset.disabled = false;
            saveButton.disabled = false;
        } catch (error) {
            console.error('Upload error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Upload Error',
                text: error.message,
            });
            uploadStatusDiv.innerHTML = `<p style="color: #dc3545;"><strong>Upload error:</strong> ${error.message}</p>`;
        }
    });

    // Handles the final submission of the video metadata form.
    createForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        if (!storageIdentifierInput.value) {
            Swal.fire('Attention', 'Please upload a video file first.', 'warning');
            return;
        }
        saveButton.disabled = true;
        saveButton.textContent = 'Saving...';

        const formData = new FormData(createForm);

        try {
            const response = await fetch('/api/admin/videos', {
                method: 'POST',
                body: formData
            });
            if (!response.ok) {
                const errorData = await response.json();
                const errorMessages = Object.values(errorData.errors || {}).flat().join('\n');
                throw new Error(errorMessages || errorData.message || 'An error occurred while saving the data.');
            }

            await Swal.fire({
                title: 'Success!',
                text: 'Video registered successfully!',
                icon: 'success'
            });

            document.dispatchEvent(new CustomEvent('reloadAllVideos'));
            if (courseSelect.value === 'new_course') {
                await loadCoursesIntoSelect();
            }
            createForm.reset();
            newCourseInput.style.display = 'none';
            metadataFieldset.disabled = true;
            saveButton.disabled = true;
            uploadStatusDiv.innerHTML = '';
        } catch (error) {
            console.error('Error saving metadata:', error);
            Swal.fire('Save Error', error.message, 'error');
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = 'Save Video';
        }
    });

    /**
     * Handles the submission of the video edit form.
     */
    editForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        const videoId = editVideoIdInput.value;
        const formData = new FormData(editForm);

        try {
            const response = await fetch(`/api/admin/videos/${videoId}`, {
                method: 'PUT',
                body: formData
            });
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Failed to update the video.');
            }

            Swal.fire({
                title: 'Updated!',
                text: 'Video updated successfully!',
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });

            closeEditModal();
            document.dispatchEvent(new CustomEvent('reloadAllVideos'));
        } catch (error) {
            console.error('Error updating video:', error);
            Swal.fire('Error!', error.message, 'error');
        }
    });

    /**
     * Sets up a file input to show a preview of the selected image.
     * @param {HTMLInputElement} fileInput - The file input element.
     * @param {HTMLImageElement} previewImage - The image element to display the preview.
     */
    function setupThumbnailPreview(fileInput, previewImage) {
        fileInput.addEventListener('change', () => {
            const file = fileInput.files[0];
            if (file) {
                previewImage.src = URL.createObjectURL(file);
                previewImage.style.display = 'block';
            } else {
                previewImage.style.display = 'none';
            }
        });
    }

    /**
     * Fetches the list of courses and populates the course selection dropdown.
     */
    async function loadCoursesIntoSelect() {
        try {
            const response = await fetch('/api/admin/courses');
            if (!response.ok) throw new Error('Could not fetch courses.');
            const courses = await response.json();

            // Clear existing options except the first two ("Select" and "New")
            while (courseSelect.options.length > 2) {
                courseSelect.remove(2);
            }

            courses.forEach(course => {
                const option = new Option(course.name, course.name);
                courseSelect.add(option);
            });
        } catch (error) {
            console.error('Error loading courses for select dropdown:', error);
        }
    }

    // Shows/hides the "New Course Name" input based on dropdown selection.
    courseSelect.addEventListener('change', function () {
        const isNew = this.value === 'new_course';
        newCourseInput.style.display = isNew ? 'block' : 'none';
        newCourseInput.required = isNew;
        if (!isNew) {
            newCourseInput.value = '';
        }
    });

    // Initial setup calls
    setupThumbnailPreview(createThumbnailInput, createThumbnailPreview);
    setupThumbnailPreview(editThumbnailInput, editThumbnailPreview);
    loadCoursesIntoSelect();
    resetAndLoadCrud();
});