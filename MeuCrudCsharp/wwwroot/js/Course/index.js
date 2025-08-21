/**
 * @file Manages the client-side logic for the main course listing page.
 * This includes fetching and caching course data, rendering a feature carousel,
 * displaying course and video lists, and handling the "Continue Watching" feature.
 */
document.addEventListener('DOMContentLoaded', function () {
    // --- DOM Element Selections ---
    const carouselWrapper = document.getElementById('carousel-wrapper');
    const coursesContainer = document.getElementById('courses-section-container');
    const pageLoader = document.getElementById('page-loader');

    // Substitua o sessionCache por estas variáveis
    let currentPage = 1;
    const pageSize = 5; // O mesmo valor do backend
    let isLoading = false;
    let hasMorePages = true;
    let swiperInstance; // Mantenha esta

    // --- Rendering Functions ---

    /**
     * Creates the HTML for a single video card.
     * @param {object} video - The video data object.
     * @param {string} courseId - The ID of the course the video belongs to.
     * @returns {string} The HTML string for the video card.
     */
    function createVideoCard(video, courseId) {
        // The backend should provide duration in a consistent format, but we handle it here.
        // Assuming video.duration is a TimeSpan string like "00:05:30" or a number in seconds.
        let durationMinutes = 0;
        if (typeof video.duration === 'string' && video.duration.includes(':')) {
            const parts = video.duration.split(':');
            durationMinutes = parseInt(parts[0], 10) * 60 + parseInt(parts[1], 10);
        } else if (typeof video.duration === 'number') {
            durationMinutes = Math.floor(video.duration / 60);
        }
        const durationText = `${durationMinutes} min`;

        const thumbnailUrl = video.thumbnailUrl || `https://placehold.co/600x400/111111/FFFFFF?text=${encodeURIComponent(video.title)}`;
        const videoPageUrl = `/Videos/Index?videoId=${video.id}&courseId=${courseId}`;

        return `
            <a href="${videoPageUrl}" class="video-card" data-video-id="${video.id}" data-course-id="${courseId}">
                <div class="video-thumbnail" style="background-image: url('${thumbnailUrl}')">
                    <i class="fas fa-play play-icon"></i>
                </div>
                <div class="video-info">
                    <h3 class="video-title">${video.title}</h3>
                    <p class="video-duration">${durationText}</p>
                </div>
            </a>
        `;
    }

    /**
     * Renders the "Continue Watching" section if a last-watched video is found in local storage.
     */
    function renderContinueWatching() {
        const lastWatchedVideoJson = localStorage.getItem('lastWatchedVideo');
        if (!lastWatchedVideoJson) return;

        const videoData = JSON.parse(lastWatchedVideoJson);

        // Ensure the required data exists before rendering.
        if (!videoData || !videoData.courseId) {
            return;
        }

        const continueWatchingHTML = `
            <div id="continue-watching-row" class="course-row">
                <h2 class="course-row-title">Continue Watching</h2>
                <div class="videos-scroller">
                    ${createVideoCard(videoData, videoData.courseId)}
                </div>
            </div>
        `;
        coursesContainer.insertAdjacentHTML('afterbegin', continueWatchingHTML);
    }

    /**
 * ATUALIZADO: renderCourseRows agora anexa o conteúdo em vez de substituir
 * @param {Array<object>} courses - Uma lista de cursos para ANEXAR ao container.
 */
    function renderCourseRows(courses) {
        let coursesHTML = '';
        courses.forEach(course => {
            if (course.videos && course.videos.length > 0) {
                coursesHTML += `
                    <div id="course-${course.id}" class="course-row">
                        <h2 class="course-row-title">${course.name}</h2>
                        <div class="videos-scroller">
                            ${course.videos.map(video => createVideoCard(video, course.id)).join('')}
                        </div>
                    </div>
                `;
            }
        });
        coursesContainer.insertAdjacentHTML('beforeend', coursesHTML);
    }

    /**
 * NOVO: Busca uma página de cursos e os renderiza.
 */
    async function fetchAndRenderCourses() {
        if (isLoading || !hasMorePages) return; // Previne chamadas duplicadas
        isLoading = true;
        pageLoader.style.display = 'block'; // Mostra o loader

        try {
            const response = await fetch(`/api/courses/paginated?pageNumber=${currentPage}&pageSize=${pageSize}`);
            if (!response.ok) throw new Error('Network error');

            const paginatedResult = await response.json();
            const { items, totalCount } = paginatedResult;

            if (items && items.length > 0) {
                // Se for a primeira página, renderiza o carrossel também
                if (currentPage === 1) {
                    renderCarousel(items);
                    initSwiper();
                }
                renderCourseRows(items);
                currentPage++; // Prepara para a próxima página
                // Verifica se ainda há mais páginas para carregar
                hasMorePages = (currentPage - 1) * pageSize < totalCount;
            } else {
                hasMorePages = false; // Não há mais itens
            }

        } catch (error) {
            console.error("Failed to load courses:", error);
            coursesContainer.innerHTML += `<p class="error-message">Failed to load more courses.</p>`;
        } finally {
            isLoading = false;
            pageLoader.style.display = 'none'; // Esconde o loader
        }
    }

    /**
     * Creates the carousel slides based on the course data.
     * @param {Array<object>} courses - The list of courses from the API.
     */
    function renderCarousel(courses) {
        // Filter for courses that have videos to ensure a thumbnail is available.
        const coursesWithVideos = courses.filter(course => course.videos && course.videos.length > 0);

        // Create the HTML for each slide.
        const slidesHTML = coursesWithVideos.map(course => {
            const firstVideo = course.videos[0];
            // Use the first video's thumbnail as the carousel image.
            const thumbnailUrl = firstVideo.thumbnailUrl || `https://placehold.co/1280x720/000000/FFFFFF?text=${encodeURIComponent(course.name)}`;
            // The slide links to the first video of the course.
            const videoPageUrl = `/Videos/Index?videoId=${firstVideo.id}&courseId=${course.id}`;

            return `
            <a href="${videoPageUrl}" class="swiper-slide">
                <img src="${thumbnailUrl}" loading="lazy" alt="Banner para o curso ${course.name}" class="carousel-image"/>
                <div class="carousel-caption">
                    <h2 class="carousel-title">${course.name}</h2>
                    <p class="carousel-description">Watch the first video now!</p>
                </div>
            </a>
        `;
        }).join('');

        // Insert the generated slides into the carousel wrapper.
        if (carouselWrapper) {
            carouselWrapper.innerHTML = slidesHTML;
        }
    }

    /**
     * Initializes the Swiper.js library with the desired settings.
     * This should be called AFTER renderCarousel() has inserted the slides into the DOM.
     */
    function initSwiper() {
        // Destroy a previous instance to avoid duplicate initializations.
        if (swiperInstance) {
            swiperInstance.destroy(true, true);
        }

        // Create the new Swiper instance.
        swiperInstance = new Swiper('.swiper', {
            loop: true,
            autoplay: {
                delay: 5000, // Switch to the next slide every 5 seconds
                disableOnInteraction: false, // Continue autoplay after user interaction
            },
            pagination: {
                el: '.swiper-pagination', // Pagination element (the dots)
                clickable: true, // Allow clicking on dots to navigate
            },
            navigation: {
                nextEl: '.swiper-button-next', // "Next" button element
                prevEl: '.swiper-button-prev', // "Previous" button element
            },
            effect: 'fade', // Transition effect
            fadeEffect: {
                crossFade: true // Prevents flickering during the fade effect
            },
            grabCursor: true, // Shows the "grab" cursor on hover
        });
    }

    /**
 * NOVO: Lógica para a rolagem infinita usando Intersection Observer.
 * É mais eficiente que ficar escutando o evento 'scroll'.
 */
    function setupInfiniteScroll() {
        const observer = new IntersectionObserver((entries) => {
            // A 'entry' é o elemento que estamos observando (o loader)
            const firstEntry = entries[0];
            if (firstEntry.isIntersecting && hasMorePages) {
                fetchAndRenderCourses(); // Carrega mais cursos quando o loader fica visível
            }
        }, {
            rootMargin: '100px' // Começa a carregar 100px antes do loader aparecer na tela
        });

        // Começa a observar o elemento loader
        observer.observe(pageLoader);
    }

    // --- Page Initialization ---
    renderContinueWatching();
    fetchAndRenderCourses(); 
    setupInfiniteScroll(); 
});