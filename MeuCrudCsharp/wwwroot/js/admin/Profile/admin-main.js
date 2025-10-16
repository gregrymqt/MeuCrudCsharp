// /js/admin/admin-main.js

// 1. Importe a nova função junto com as outras
import { initializeSidebar, initializeMenuToggle } from './modules/ui/navigation.js';
import { initializePlansPanel } from './modules/panels/plansPanel.js';
import {initializeCoursesPanel} from './modules/panels/coursesPanel.js';
import {initializeStudentsPanel} from './modules/panels/studentsPanel.js';
import { loadSubscriptions } from './modules/panels/subscriptionsPanel.js';
import { initializeTabs } from './modules/ui/tabs.js';

async function main() {
    const panelLoaders = {
        'nav-plans': initializePlansPanel,
        'nav-courses': await initializeCoursesPanel,
        'nav-students': await initializeStudentsPanel,
        'nav-subscriptions': loadSubscriptions,
    };

    initializeMenuToggle();
    initializeSidebar(panelLoaders);
    initializeTabs();

    document.querySelector('.sidebar-link.active')?.click();
}

document.addEventListener('DOMContentLoaded', await main);