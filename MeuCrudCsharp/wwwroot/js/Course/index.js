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
        const sessionCache = {}; // Cache simples para a sessão atual
        let swiperInstance;

        // =====================================================================
        // Funções de Renderização (Criação do HTML)
        // =====================================================================

        // Cria um único card de vídeo
        function createVideoCard(video) {
            // Formata a duração (ex: de 900 segundos para "15 min")
            const durationMinutes = Math.floor(video.duration.totalSeconds / 60);
            const durationText = `${durationMinutes} min`;

            return `
                <a href="/videos/${video.storageIdentifier}" class="video-card" data-video-id="${video.id}">
                    <div class="video-thumbnail" style="background-image: url('https://placehold.co/600x400/111111/FFFFFF?text=${encodeURIComponent(video.title)}')">
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
            // Tenta pegar o último vídeo assistido do localStorage
            const lastWatchedVideoJson = localStorage.getItem('lastWatchedVideo');
            if (!lastWatchedVideoJson) return;

            const video = JSON.parse(lastWatchedVideoJson);

            const continueWatchingHTML = `
                <div id="continue-watching-row" class="course-row">
                    <h2 class="course-row-title">Continuar Assistindo</h2>
                    <div class="videos-scroller">
                        ${createVideoCard(video)}
                    </div>
                </div>
            `;
            // Insere esta seção logo no início do container de cursos
            coursesContainer.insertAdjacentHTML('afterbegin', continueWatchingHTML);
        }

        // Cria e popula o carrossel
        function renderCarousel(courses) {
            let carouselHTML = '';
            courses.forEach((course, index) => {
                carouselHTML += `
                    <div class="swiper-slide" style="background-image: url('https://placehold.co/1920x1080/${Math.floor(Math.random()*16777215).toString(16)}/FFFFFF?text=${encodeURIComponent(course.name)}')">
                        <div class="slide-content">
                            <h2>${course.name}</h2>
                            <a href="#course-${course.id}" class="btn">Ver Curso</a>
                        </div>
                    </div>
                `;
            });
            carouselWrapper.innerHTML = carouselHTML;

            // Inicializa ou atualiza o Swiper
            if (swiperInstance) {
                swiperInstance.update();
            } else {
                swiperInstance = new Swiper('.swiper', {
                    loop: true,
                    pagination: { el: '.swiper-pagination', clickable: true },
                    navigation: { nextEl: '.swiper-button-next', prevEl: '.swiper-button-prev' },
                    autoplay: { delay: 5000, disableOnInteraction: false },
                });
            }
        }

        // Cria todas as fileiras de cursos e seus vídeos
        function renderCourseRows(courses) {
            let coursesHTML = '';
            courses.forEach(course => {
                // Só cria a fileira se o curso tiver vídeos
                if (course.videos && course.videos.length > 0) {
                    coursesHTML += `
                        <div id="course-${course.id}" class="course-row">
                            <h2 class="course-row-title">${course.name}</h2>
                            <div class="videos-scroller">
                                ${course.videos.map(createVideoCard).join('')}
                            </div>
                        </div>
                    `;
                }
            });
            // Adiciona o HTML gerado ao container principal
            coursesContainer.innerHTML += coursesHTML;
        }


        // =====================================================================
        // Função Principal para Buscar e Exibir os Dados
        // =====================================================================
        async function loadPageContent() {
            // 1. Verifica o cache primeiro
            if (sessionCache['allCourses']) {
                console.log("Carregando dados do cache da sessão.");
                const data = sessionCache['allCourses'];
                renderCarousel(data);
                renderCourseRows(data);
                pageLoader.style.display = 'none'; // Esconde o loader
                return;
            }

            // 2. Se não houver cache, busca na API
            try {
                // Este é o endpoint que seu backend C# precisa criar
                const response = await fetch('/api/courses/all'); 
                if (!response.ok) {
                    throw new Error(`Erro na rede: ${response.statusText}`);
                }
                const coursesData = await response.json();

                // Armazena os dados no cache para futuras visitas na mesma sessão
                sessionCache['allCourses'] = coursesData;

                // Renderiza os componentes na tela
                renderCarousel(coursesData);
                renderCourseRows(coursesData);

            } catch (error) {
                console.error("Falha ao carregar o conteúdo da página:", error);
                coursesContainer.innerHTML = `<div class="loader" style="color: #e50914;">Falha ao carregar cursos. Tente novamente mais tarde.</div>`;
            } finally {
                // Esconde o loader, mesmo que tenha dado erro
                pageLoader.style.display = 'none';
            }
        }

        // =====================================================================
        // Ponto de Entrada: Inicia tudo
        // =====================================================================
        
        // Primeiro, renderiza a seção "Continuar Assistindo", se aplicável
        renderContinueWatching();
        
        // Em seguida, carrega todo o resto do conteúdo (cursos e vídeos)
        loadPageContent();

    });