/*
 * AUDIT HEADER
 * File: script.js
 * Module: Website Logic
 * Created: 2026-04-21
 * Author: Zane Stanton
 *
 * CHANGELOG:
 * 2026-04-21 - 1.0.0 - Initial implementation of catalog and interaction logic.
 * 2026-04-28 - 1.2.6 - Updated build version and download references.
 * 2026-05-02 - 1.2.6 - Synced website metadata with v1.2.6 release.
 */

document.addEventListener('DOMContentLoaded', () => {



    // ========================================
    // PARTICLES BACKGROUND (CANVAS)
    // ========================================
    const canvas = document.getElementById('particles');
    if (canvas && canvas.getContext) {
        const ctx = canvas.getContext('2d');
        let width = canvas.width = window.innerWidth;
        let height = canvas.height = window.innerHeight;

        window.addEventListener('resize', () => {
            width = canvas.width = window.innerWidth;
            height = canvas.height = window.innerHeight;
        });

        const particles = [];
        for (let i = 0; i < 30; i++) {
            particles.push({
                x: Math.random() * width,
                y: Math.random() * height,
                radius: Math.random() * 2 + 1,
                vx: (Math.random() - 0.5) * 0.5,
                vy: (Math.random() - 0.5) * 0.5 - 0.5
            });
        }

        function drawParticles() {
            ctx.clearRect(0, 0, width, height);
            ctx.fillStyle = 'rgba(161, 85, 255, 0.4)';
            ctx.beginPath();
            particles.forEach(p => {
                p.x += p.vx;
                p.y += p.vy;
                if (p.y < -10) p.y = height + 10;
                if (p.x < -10) p.x = width + 10;
                if (p.x > width + 10) p.x = -10;
                
                ctx.moveTo(p.x, p.y);
                ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
            });
            ctx.fill();
            requestAnimationFrame(drawParticles);
        }
        drawParticles();
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
                'Install Office 2024 (Build 2024) — Office 2024 deployment',
                'Office-C2R-Install — Office Click-to-Run installer',
                'Backup and Restore Activation State 1.0.0 — License state management',
                'Christitus Utility — Chris Titus Tech Windows Utility',
                'CCleaner 6.40.115.62 — System cleaning tool',
                'Macrium Reflect X 10.0.8843 — Disk imaging & backup',
                'CompactGUI — Windows file compression tool',
                'Defragger — Disk defragmentation utility',
                'Ninite Installer — Bulk application installer',
                'Rufus — Bootable USB creation tool',
                'Visual C++ AIO — Visual C++ Redistributable package',
                'PC Repair Suite 2.0.0 — All-in-one repair suite',
                'IObit Driver Booster PRO 13.4.0.234 — Driver updater',
                'Dell OS Recovery Tool 2.3.4.3569 — Dell recovery',
                'CleanMyPc 1.11.1.2079 — PC optimization tool',
                'EaseUS Partition Master 20.3.0 Build 202604081519 — Partition management tool',
                'UniGetUI 2026.1.9 — Package Manager UI'
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

    document.querySelectorAll('.feature-card, .stat-card, .module-detail-card').forEach((el, i) => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = `all 0.6s cubic-bezier(0.4, 0, 0.2, 1) ${i * 0.1}s`;
        observer.observe(el);
    });

});
