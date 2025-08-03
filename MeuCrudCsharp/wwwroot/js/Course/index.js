document.addEventListener('DOMContentLoaded', function () {

    // =====================================================================
    // Seleção de Elementos DOM
    // =====================================================================
    const carouselWrapper = document.getElementById('carousel-wrapper');
    const coursesContainer = document.getElementById('courses-section-container');
    const pageLoader = document.getElementById('page-loader');

    // =====================================================================
    // Estado e Cache
    // =====================================================================
    const sessionCache = {};
    let swiperInstance;

    // =====================================================================
    // Funções de Renderização (Criação do HTML)
    // =====================================================================

    // MUDANÇA 1: A função agora aceita o 'courseId' para criar o link correto.
    function createVideoCard(video, courseId) {
        // MUDANÇA 2: Usando a nova propriedade 'durationInSeconds'.
        const durationMinutes = Math.floor(video.durationInSeconds / 60);
        const durationText = `${durationMinutes} min`;

        // MUDANÇA 3: Usando a nova propriedade 'thumbnailUrl'.
        const thumbnailUrl = video.thumbnailUrl || `https://placehold.co/600x400/111111/FFFFFF?text=${encodeURIComponent(video.title)}`;

        // MUDANÇA 4: Criando o link dinâmico que você pediu.
        // A sintaxe correta é /PageName?param1=valor1&param2=valor2
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

    // Cria a seção "Continuar Assistindo"
    function renderContinueWatching() {
        const lastWatchedVideoJson = localStorage.getItem('lastWatchedVideo');
        if (!lastWatchedVideoJson) return;

        const videoData = JSON.parse(lastWatchedVideoJson);

        // Ponto de Atenção: O 'courseId' precisa estar salvo no localStorage.
        const continueWatchingHTML = `
            <div id="continue-watching-row" class="course-row">
                <h2 class="course-row-title">Continuar Assistindo</h2>
                <div class="videos-scroller">
                    ${createVideoCard(videoData, videoData.courseId)}
                </div>
            </div>
        `;
        coursesContainer.insertAdjacentHTML('afterbegin', continueWatchingHTML);
    }

    // Cria todas as fileiras de cursos e seus vídeos
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
        coursesContainer.innerHTML += coursesHTML;
    }

    // As outras funções (renderCarousel, loadPageContent) permanecem as mesmas...

    // =====================================================================
    // Função Principal para Buscar e Exibir os Dados
    // =====================================================================
    async function loadPageContent() {
        if (sessionCache['allCourses']) {
            const data = sessionCache['allCourses'];
            renderCarousel(data);
            renderCourseRows(data);
            pageLoader.style.display = 'none';
            return;
        }

        try {
            // O endpoint que criamos no C#
            const response = await fetch('/api/courses/all');
            if (!response.ok) {
                throw new Error(`Erro na rede: ${response.statusText}`);
            }
            const coursesData = await response.json();
            sessionCache['allCourses'] = coursesData;

            renderCarousel(coursesData);
            renderCourseRows(coursesData);

        } catch (error) {
            console.error("Falha ao carregar o conteúdo da página:", error);
            coursesContainer.innerHTML = `<div class="loader" style="color: #e50914;">Falha ao carregar cursos. Tente novamente mais tarde.</div>`;
        } finally {
            pageLoader.style.display = 'none';
        }
    }

    // (O resto do seu JS continua aqui: renderCarousel, inicialização do Swiper, etc.)

    // =====================================================================
    // Ponto de Entrada
    // =====================================================================
    renderContinueWatching();
    loadPageContent();
});