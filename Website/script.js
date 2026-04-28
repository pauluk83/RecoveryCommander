document.addEventListener('DOMContentLoaded', () => {



    // ========================================
    // PARTICLES BACKGROUND
    // ========================================
    const particlesContainer = document.getElementById('particles');
    for (let i = 0; i < 20; i++) {
        const p = document.createElement('div');
        p.className = 'particle';
        const size = Math.random() * 4 + 2;
        p.style.width = size + 'px';
        p.style.height = size + 'px';
        p.style.left = Math.random() * 100 + '%';
        p.style.top = Math.random() * 100 + '%';
        p.style.animationDelay = Math.random() * 15 + 's';
        p.style.animationDuration = (Math.random() * 10 + 10) + 's';
        particlesContainer.appendChild(p);
    }

    // ========================================
    // MOBILE MENU
    // ========================================
    const mobileBtn = document.getElementById('mobile-menu-btn');
    const navLinks = document.getElementById('nav-links');

    if (mobileBtn) {
        mobileBtn.addEventListener('click', () => {
            navLinks.classList.toggle('active');
            mobileBtn.classList.toggle('active');
        });
    }

    // Close menu on link click
    document.querySelectorAll('.nav-links a').forEach(link => {
        link.addEventListener('click', () => {
            navLinks.classList.remove('active');
            mobileBtn.classList.remove('active');
        });
    });

    // ========================================
    // MODULE TAB SWITCHING
    // ========================================
    const tabBtns = document.querySelectorAll('.tab-btn');

    const moduleData = {
        dism: {
            title: 'DISM',
            subtitle: 'Deployment Image Servicing and Management (DISM) operations.',
            features: [
                'CheckHealth — Check image health (/Online /Cleanup-Image /CheckHealth)',
                'ScanHealth — Scan image health (/Online /Cleanup-Image /ScanHealth)',
                'RestoreHealth — Restore image health (/Online /Cleanup-Image /RestoreHealth)',
                'StartComponentCleanup — Component cleanup (/Online /Cleanup-Image /StartComponentCleanup)'
            ],
            badge: 'Plugin-based Architecture'
        },
        reagentc: {
            title: 'REAgentC',
            subtitle: 'Manages the Windows Recovery Environment (WinRE) including status and linkage repair.',
            features: [
                'Check Status — Query WinRE Status (/info)',
                'Reset Recovery — Reset WinRE Link (Disable/Enable Cycle)',
                'Enable WinRE — Enable WinRE (/enable)',
                'Disable WinRE — Disable WinRE (/disable)',
                'Repair WinRE Path — Advanced Repair (Mount/Pick/Set)',
                'Complete PBR Setup Wizard — Guided Push-Button Reset Setup',
                'Register FFU Restore — Register Modern FFU Factory Image'
            ],
            badge: 'System Core Module'
        },
        sfc: {
            title: 'System File Checker',
            subtitle: 'Verifies and repairs system file integrity using SFC.',
            features: [
                'Scan Now — Full System Scan (/scannow)',
                'Verify Only — Verification Only (/verifyonly)',
                'Offline Scan — Offline System Scan on external Windows installations'
            ],
            badge: 'Diagnostic Tool'
        },
        diagnostics: {
            title: 'Diagnostics',
            subtitle: 'Comprehensive system diagnostic toolkit for identifying hardware and software issues.',
            features: [
                'Full Diagnostic — Run all checks and generate a comprehensive report',
                'System Info — Detailed system hardware and OS information',
                'CPU Details — CPU model, cores, and logical processor count',
                'RAM Details — Installed RAM capacity and speed',
                'Disk Health — SMART health status of physical drives',
                'Storage Space — Free space and filesystem type for all drives',
                'Network Config — All network adapter settings and IPs',
                'Active Connections — Active network connections and listening ports',
                'Startup Programs — Applications configured to run at startup',
                'Running Processes — Currently running processes and services',
                'File Integrity — System file integrity verification'
            ],
            badge: 'Technician Toolkit'
        },
        utilities: {
            title: 'Utilities',
            subtitle: 'Collection of utility tools for system maintenance and software installation.',
            features: [
                'Activation — Windows Activation using trusted scripts',
                'Install Office 2024 — Office 2024 deployment',
                'Office-C2R-Install — Office Click-to-Run installer',
                'Backup and Restore Activation State — License state management',
                'Christitus Utility — Chris Titus Tech Windows Utility',
                'CCleaner 6.40.115.62 — System cleaning tool',
                'Macrium Reflect Portable — Disk imaging & backup',
                'CompactGUI — Windows file compression tool',
                'Defragger — Disk defragmentation utility',
                'Ninite Installer — Bulk application installer',
                'Rufus — Bootable USB creation tool',
                'Visual C++ AIO — Visual C++ Redistributable package',
                'PC Repair Suite — All-in-one repair suite',
                'IObit Driver Booster PRO 13.4.0.234 — Driver updater',
                'Dell OS Recovery Tool v2.3.4.3569 — Dell recovery',
                'CleanMyPc v1.12.2.2178 — PC optimization tool'
            ],
            badge: 'Toolbox'
        },
        systemprep: {
            title: 'System Prep',
            subtitle: 'Performs various system preparation and cleanup tasks using modular services.',
            features: [
                'Full System Prep — Run all maintenance tasks sequentially',
                'Upgrade Winget Packages — Updates programs via Winget (Selective)',
                'Update Store Apps — Updates Microsoft Store packages (Selective)',
                'Update PS Modules — Updates PowerShell Modules (Selective)',
                'Scan for Windows Updates — Check and install OS updates (Selective)',
                'Clear All Caches — Removes browser caches and temp files',
                'Deep Clean WinSxS — Component store cleanup (resetbase)',
                'Apply Privacy Tweaks — Disable telemetry and web search in Start',
                'Run Disk Cleanup — Standard cleanmgr /sagerun:65535'
            ],
            badge: 'Maintenance'
        },
        malware: {
            title: 'Malware Removal',
            subtitle: 'Collection of antivirus and malware removal tools.',
            features: [
                'Emsisoft Emergency Kit — Portable malware scanner',
                'KVRT — Kaspersky Virus Removal Tool',
                'AdwCleaner — Malwarebytes AdwCleaner',
                'Microsoft MSRT — Malicious Software Removal Tool',
                'ESET Online Scanner — ESET cloud-based scanner',
                'Norton Power Eraser — Aggressive threat remover',
                'HitmanPro — Second-opinion malware scanner',
                'ClamWin Portable — Portable antivirus',
                'F-Secure Online Scanner — F-Secure cloud scanner',
                'Trend Micro HouseCall — Online security scan',
                'Sophos Scan & Clean — Sophos removal tool',
                'Comodo Cleaning Essentials — Comodo malware cleaner',
                'Dr.Web CureIt! — Dr.Web portable scanner',
                'SuperAntiSpyware Portable — Spyware removal tool'
            ],
            badge: 'Security Suite'
        },
        drivers: {
            title: 'Driver Manager',
            subtitle: 'Comprehensive driver backup, restoration, and optimization tools.',
            features: [
                'Backup Drivers — Exports all third-party drivers to a selected folder',
                'Restore Drivers — Installs drivers from a folder (pnputil)',
                'List Drivers — Enumerate all installed third-party drivers',
                'Cleanup Driver Store — Removes redundant and old driver versions',
                'IObit Driver Booster PRO 13.4.0.234 — Automated driver updater'
            ],
            badge: 'Driver Tools'
        },
        cloud: {
            title: 'Cloud Recovery',
            subtitle: 'Cloud-based system restoration and configuration backup.',
            features: [
                'Trigger Cloud Reset — Initiate Windows Cloud Download and Reset',
                'Backup Profile to Cloud — Sync user profile settings to cloud storage',
                'Restore Profile from Cloud — Download and apply profile from cloud',
                'Configure Cloud Account — Set up OneDrive/GitHub integration for backups'
            ],
            badge: 'Cloud Sync'
        }
    };

    function updateModuleContent(moduleId) {
        const data = moduleData[moduleId];
        const card = document.querySelector('.module-detail-card');
        
        card.style.opacity = '0';
        card.style.transform = 'translateY(10px)';
        
        setTimeout(() => {
            document.querySelector('.module-title-group h3').textContent = data.title;
            document.querySelector('.module-title-group p').textContent = data.subtitle;
            
            const featuresList = document.querySelector('.module-features ul');
            featuresList.innerHTML = data.features.map(f => `<li>${f}</li>`).join('');
            
            document.querySelector('.badge-mini').textContent = data.badge;
            
            card.style.opacity = '1';
            card.style.transform = 'translateY(0)';
        }, 300);
    }

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            tabBtns.forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            updateModuleContent(btn.getAttribute('data-tab'));
        });
    });

    // ========================================
    // SCROLL EFFECTS
    // ========================================
    window.addEventListener('scroll', () => {
        const header = document.querySelector('#main-header');
        if (window.scrollY > 50) {
            header.style.background = 'rgba(10, 10, 11, 0.95)';
            header.style.boxShadow = '0 4px 20px rgba(0,0,0,0.3)';
        } else {
            header.style.background = 'rgba(10, 10, 11, 0.8)';
            header.style.boxShadow = 'none';
        }
    });

    // Reveal on scroll
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, { threshold: 0.1 });

    document.querySelectorAll('.feature-card, .stat-card, .transfer-card, .module-detail-card').forEach((el, i) => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = `all 0.6s cubic-bezier(0.4, 0, 0.2, 1) ${i * 0.1}s`;
        observer.observe(el);
    });

    // ========================================
    // FILE SHARE — LOCAL FILES DIRECTORY
    // ========================================
    const downloadsGrid = document.getElementById('downloads-grid');
    const loadingState = document.getElementById('loading-state');
    const emptyState = document.getElementById('empty-state');
    const searchInput = document.getElementById('search-files');

    // Hosted files from InfinityFree /htdocs/files/
    const allFiles = [
        { 
            title: 'MacPaw CleanMyPC', 
            description: 'MacPaw CleanMyPC Portable', 
            fileUrl: 'https://www.dropbox.com/scl/fi/bowosjzxmkr16qw5zcfnc/CleanMyPC.txt?rlkey=sz2tafebh67sp6aod6hxjd0pp&st=2be8vsyw&dl=1', 
            fileSize: '80.4 MB', 
            category: 'Utilities',
            lastUpdated: '17/03/2026',
            buildNumber: '1.11.1.2079'
        },
        { 
            title: 'Driver Booster PRO', 
            description: 'Driver Booster PRO Portable', 
            fileUrl: 'https://www.dropbox.com/scl/fi/2paq4t1yevyprkw5jrp0a/DriverBoosterPortable.txt?rlkey=p6j6ofauo26tp5xnunqhc3pxb&st=bvyegici&dl=1', 
            fileSize: '32.6 MB', 
            category: 'Utilities',
            lastUpdated: '17/03/2026',
            buildNumber: '13.4.0.234'
        },
        { 
            title: 'Dell OS Recovery Tool', 
            description: 'Dell OS Recovery Tool Portable', 
            fileUrl: '/files/Dell%20OS%20Recovery%20Toolv2.3.4.3569.txt', 
            fileSize: '122 MB', 
            category: 'Utilities',
            lastUpdated: '17/03/2026',
            buildNumber: '2.3.4.3569'
        },
        { 
            title: 'PC Repair Suite', 
            description: 'PC Repair Suite Portable', 
            fileUrl: 'https://www.dropbox.com/scl/fi/77a4s205yz7qjocfmev9r/PCRepairSuite.txt?rlkey=us4dw96qvw0bpqhemrghs5ru8&st=q6d07jhd&dl=1', 
            fileSize: '53.4 MB', 
            category: 'Utilities',
            lastUpdated: '16/03/2026',
            buildNumber: '2.0.0'
        },
        { 
            title: 'Macrium Reflect X', 
            description: 'Macrium Reflect X Portable', 
            fileUrl: 'https://www.dropbox.com/scl/fi/qj5pa6ykrara7jcb8m3k1/Macrium.txt?rlkey=9gl0l7pgsuniits2gmovb44oi&st=escodys6&dl=1', 
            fileSize: '18.6 MB', 
            category: 'Utilities',
            lastUpdated: '16/03/2026',
            buildNumber: '10.0.8843'
        },
        { 
            title: 'CCleaner', 
            description: 'CCleaner portable', 
            fileUrl: 'https://www.dropbox.com/scl/fi/7op61jbtwy3nc50i3qu0v/CCleaner-6.40.115.62.txt?rlkey=4vel6tocnd3hmpucb1lsmu8s9&st=l1ox1unw&dl=1', 
            fileSize: '54.6 MB', 
            category: 'Utilities',
            lastUpdated: '26/04/2026',
            buildNumber: '6.40.115.62'
        },
        { 
            title: 'Office 2024', 
            description: 'Microsoft Office 2024 Deployment', 
            fileUrl: 'https://www.dropbox.com/scl/fi/i6ds50lst2edbuc2ildlv/office2024..txt?rlkey=dbhl2bo5y3tj5a7sfjnadeu9y&st=oesmjnyr&dl=1', 
            fileSize: '4.2 MB', 
            category: 'Utilities',
            lastUpdated: '28/04/2026',
            buildNumber: '2024'
        },
        { 
            title: 'Backup & Restore Activation', 
            description: 'Windows License State Management', 
            fileUrl: 'https://www.dropbox.com/scl/fi/y3a8j6osto9rpip7buveh/Backup-Activation.txt?rlkey=dd1ms4f8wjz3iav9g13ewnzbw&st=6ex79f52&dl=1', 
            fileSize: '1.2 MB', 
            category: 'Utilities',
            lastUpdated: '28/04/2026',
            buildNumber: '1.0.0'
        }
    ];

    function initFiles() {
        renderFiles(allFiles);
    }

    function renderFiles(files) {
        if (loadingState) loadingState.style.display = 'none';

        if (files.length === 0) {
            downloadsGrid.innerHTML = '';
            emptyState.style.display = 'flex';
            return;
        }

        emptyState.style.display = 'none';
        downloadsGrid.innerHTML = files.map((file, i) => `
            <div class="download-item" style="animation: fadeInUp 0.4s ease ${i * 0.08}s both;">
                <div class="download-item-header">
                    <div class="download-item-icon">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"></path><polyline points="13 2 13 9 20 9"></polyline></svg>
                    </div>
                    <div class="download-item-info">
                        <h4>${file.title}</h4>
                        <div class="item-meta-top">
                            <span class="category-tag">${file.category}</span>
                            ${file.buildNumber ? `<span class="version-tag">v${file.buildNumber}</span>` : ''}
                        </div>
                    </div>
                </div>
                <p class="download-item-desc">${file.description}</p>
                <div class="download-item-footer">
                    <div class="footer-left">
                        <span class="file-size">${file.fileSize || '---'}</span>
                        ${file.lastUpdated ? `<span class="item-date">${file.lastUpdated}</span>` : ''}
                    </div>
                    <div class="footer-actions">
                        <button class="icon-btn copy-file-btn" data-url="${file.fileUrl}" title="Copy Link">
                            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path></svg>
                        </button>
                        <a href="${file.fileUrl}" class="download-item-btn" target="_blank" rel="noopener">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path><polyline points="7 10 12 15 17 10"></polyline><line x1="12" y1="15" x2="12" y2="3"></line></svg>
                            Download
                        </a>
                    </div>
                </div>
            </div>
        `).join('');

        // Re-attach copy event listeners
        document.querySelectorAll('.copy-file-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const url = btn.getAttribute('data-url');
                navigator.clipboard.writeText(url).then(() => {
                    const originalHTML = btn.innerHTML;
                    btn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"></polyline></svg>';
                    btn.style.color = '#4dffdf';
                    showToast('Link copied to clipboard!');
                    setTimeout(() => {
                        btn.innerHTML = originalHTML;
                        btn.style.color = '';
                    }, 2000);
                });
            });
        });
    }

    // Search filtering
    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            const query = e.target.value.toLowerCase().trim();
            if (!query) {
                renderFiles(allFiles);
                return;
            }
            const filtered = allFiles.filter(f =>
                f.title.toLowerCase().includes(query) ||
                f.description.toLowerCase().includes(query) ||
                f.category.toLowerCase().includes(query)
            );
            renderFiles(filtered);
        });
    }

    // Add fadeInUp animation
    if (!document.getElementById('fileshare-styles')) {
        const style = document.createElement('style');
        style.id = 'fileshare-styles';
        style.textContent = `@keyframes fadeInUp { from { opacity:0; transform:translateY(16px); } to { opacity:1; transform:translateY(0); } }`;
        document.head.appendChild(style);
    }

    // Start rendering
    initFiles();

    // ========================================
    // TOAST NOTIFICATIONS
    // ========================================
    function showToast(message) {
        const existing = document.querySelector('.toast');
        if (existing) existing.remove();

        const toast = document.createElement('div');
        toast.className = 'toast';
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed; bottom: 2rem; right: 2rem;
            background: rgba(20, 20, 25, 0.95); color: #f0f0f5;
            padding: 1rem 1.5rem; border-radius: 12px;
            border: 1px solid rgba(255,255,255,0.1);
            backdrop-filter: blur(10px); font-size: 0.9rem;
            z-index: 9999; animation: toastIn 0.4s ease;
            box-shadow: 0 10px 30px rgba(0,0,0,0.4);
        `;
        document.body.appendChild(toast);
        
        // Add animation keyframes
        if (!document.getElementById('toast-styles')) {
            const style = document.createElement('style');
            style.id = 'toast-styles';
            style.textContent = `
                @keyframes toastIn { from { transform: translateY(20px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }
                @keyframes toastOut { from { transform: translateY(0); opacity: 1; } to { transform: translateY(20px); opacity: 0; } }
            `;
            document.head.appendChild(style);
        }

        setTimeout(() => {
            toast.style.animation = 'toastOut 0.3s ease forwards';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }
});
