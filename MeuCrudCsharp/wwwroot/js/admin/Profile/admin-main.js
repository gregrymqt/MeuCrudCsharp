// /js/admin/admin-main.js

import {initializeSidebar} from './modules/ui/navigation.js';
import {initializePlansPanel, loadPlans} from './modules/panels/plansPanel.js';
import {initializeCoursesPanel, loadCourses} from './modules/panels/coursesPanel.js';
import {initializeStudentsPanel, loadStudents} from './modules/panels/studentsPanel.js';
import {initializeSubscriptionsPanel} from './modules/panels/subscriptionsPanel.js';
import {initializeTabs} from './modules/ui/tabs.js';

function main() {

    const menuToggle = document.getElementById('menu-toggle');
    if (menuToggle) {
        menuToggle.addEventListener('click', () => {
            document.body.classList.toggle('sidebar-visible');
        });
    }

    const panelLoaders = {
        'nav-plans': loadPlans,
        'nav-courses': loadCourses,
        'nav-students': loadStudents,
    };

    initializeSidebar(panelLoaders);
    initializeTabs();
    initializePlansPanel();
    initializeCoursesPanel();
    initializeStudentsPanel();
    initializeSubscriptionsPanel();

    // Carrega o primeiro painel visível por padrão
    document.querySelector('.sidebar-link.active')?.click();
}

document.addEventListener('DOMContentLoaded', main);