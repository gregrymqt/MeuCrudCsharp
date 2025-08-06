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
        // Se os dados já estiverem em cache...
        if (sessionCache['allCourses']) {
            const data = sessionCache['allCourses'];
            renderCarousel(data);
            renderCourseRows(data);
            initSwiper(); // <-- CHAMAR AQUI
            pageLoader.style.display = 'none';
            return;
        }

        // Se precisar buscar os dados da API...
        try {
            const response = await fetch('/api/courses/all');
            if (!response.ok) {
                throw new Error(`Erro na rede: ${response.statusText}`);
            }
            const coursesData = await response.json();
            sessionCache['allCourses'] = coursesData;

            renderCarousel(coursesData);
            renderCourseRows(coursesData);
            initSwiper(); // <-- E CHAMAR AQUI TAMBÉM

        } catch (error) {
            console.error("Falha ao carregar o conteúdo da página:", error);
            coursesContainer.innerHTML = `<p class="error-message">Falha ao carregar cursos. Tente novamente mais tarde.</p>`;
        } finally {
            pageLoader.style.display = 'none';
        }
    }

    // =====================================================================
    // Funções de Renderização (Adicionar estas)
    // =====================================================================

    /**
     * Cria os slides do carrossel com base nos dados dos cursos.
     * @param {Array} courses - A lista de cursos vinda da API.
     */
    function renderCarousel(courses) {
        // Filtra para pegar apenas cursos que tenham vídeos, para garantir que temos uma thumbnail.
        const coursesWithVideos = courses.filter(course => course.videos && course.videos.length > 0);

        // Cria o HTML para cada slide
        const slidesHTML = coursesWithVideos.map(course => {
            const firstVideo = course.videos[0];
            // Usa a thumbnail do primeiro vídeo como imagem do carrossel
            const thumbnailUrl = firstVideo.thumbnailUrl || `https://placehold.co/1280x720/000000/FFFFFF?text=${encodeURIComponent(course.name)}`;
            // O link do slide leva para a página do primeiro vídeo do curso
            const videoPageUrl = `/Videos/Index?videoId=${firstVideo.id}&courseId=${course.id}`;

            return `
            <a href="${videoPageUrl}" class="swiper-slide">
                <img src="${thumbnailUrl}" alt="Banner para o curso ${course.name}" class="carousel-image"/>
                <div class="carousel-caption">
                    <h2 class="carousel-title">${course.name}</h2>
                    <p class="carousel-description">Assista agora ao primeiro vídeo!</p>
                </div>
            </a>
        `;
        }).join('');

        // Insere os slides gerados no wrapper do carrossel
        if (carouselWrapper) {
            carouselWrapper.innerHTML = slidesHTML;
        }
    }

    /**
     * Inicializa a biblioteca Swiper.js com as configurações desejadas.
     * Deve ser chamada DEPOIS que a função renderCarousel() inserir os slides no DOM.
     */
    function initSwiper() {
        // Destrói uma instância anterior para evitar inicializações duplicadas
        if (swiperInstance) {
            swiperInstance.destroy(true, true);
        }

        // Cria a nova instância do Swiper
        swiperInstance = new Swiper('.swiper', {
            // Opções do Swiper
            loop: true, // Cria um loop infinito
            autoplay: {
                delay: 5000, // Passa para o próximo slide a cada 5 segundos
                disableOnInteraction: false, // Continua o autoplay mesmo depois do usuário interagir
            },
            pagination: {
                el: '.swiper-pagination', // Elemento da paginação (as bolinhas)
                clickable: true, // Permite clicar nas bolinhas para navegar
            },
            navigation: {
                nextEl: '.swiper-button-next', // Elemento do botão "próximo"
                prevEl: '.swiper-button-prev', // Elemento do botão "anterior"
            },
            effect: 'fade', // Efeito de transição (pode ser 'slide', 'fade', 'cube', etc.)
            fadeEffect: {
                crossFade: true // Evita "piscadas" no efeito fade
            },
            grabCursor: true, // Mostra o ícone de "mão" ao passar o mouse
        });
    }

    // =====================================================================
    // Ponto de Entrada
    // =====================================================================
    renderContinueWatching();
    loadPageContent();
});