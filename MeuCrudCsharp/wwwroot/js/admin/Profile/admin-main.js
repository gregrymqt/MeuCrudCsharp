// /js/admin/admin-main.js

// 1. Importe a nova função junto com as outras
import { initializeSidebar, initializeMenuToggle } from './modules/ui/navigation.js';
import { loadPlans } from './modules/panels/plansPanel.js';
import { loadCourses } from './modules/panels/coursesPanel.js';
import { loadStudents } from './modules/panels/studentsPanel.js';
import { loadSubscriptions } from './modules/panels/subscriptionsPanel.js';
import { initializeTabs } from './modules/ui/tabs.js';

function main() {
    const panelLoaders = {
        'nav-plans': loadPlans,
        'nav-courses': loadCourses,
        'nav-students': loadStudents,
        'nav-subscriptions': loadSubscriptions,
    };

    initializeMenuToggle();
    initializeSidebar(panelLoaders);
    initializeTabs();

    document.querySelector('.sidebar-link.active')?.click();
}

document.addEventListener('DOMContentLoaded', main);