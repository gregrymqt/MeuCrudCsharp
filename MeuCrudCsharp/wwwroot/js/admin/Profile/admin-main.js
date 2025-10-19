// /js/admin/admin-main.js

// 1. Importe a nova função junto com as outras
import { initializeSidebar, initializeMenuToggle } from './modules/ui/navigation.js';
import { initializePlansPanel } from './modules/panels/plansPanel.js';
import {initializeCoursesPanel} from './modules/panels/coursesPanel.js';
import {initializeStudentsPanel} from './modules/panels/studentsPanel.js';
import { loadSubscriptions } from './modules/panels/subscriptionsPanel.js';
import { initializeTabs } from './modules/ui/tabs.js';
import { initializeClaimsPanel } from './modules/panels/claimsPanel.js';
import { initializeChargebacksPanel } from './modules/panels/ChargebacksPanels.js';


async function main() {
    const panelLoaders = {
        'nav-plans': initializePlansPanel,
        'nav-courses': initializeCoursesPanel,
        'nav-students': initializeStudentsPanel,
        'nav-subscriptions': loadSubscriptions,
        'nav-claims': initializeClaimsPanel,
        'nav-chargebacks': initializeChargebacksPanel,
    };

    initializeMenuToggle();
    initializeSidebar(panelLoaders);
    initializeTabs();

    // Clica no item de menu ativo para carregar o painel inicial
    document.querySelector('.sidebar-nav .active')?.click();
}

document.addEventListener('DOMContentLoaded', main);