// /js/admin/videos/modules/ui/commonUI.js
export function setupModal(modalElement, openCallback, closeCallback) {
    window.addEventListener('click', (event) => {
        if (event.target == modalElement) {
            closeCallback();
        }
    });
}

export function setupThumbnailPreview(fileInput, previewImage) {
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