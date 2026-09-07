// ==UserScript==
// @name          CCTV节目过滤脚本（精简版·仅视频）
// @namespace     http://tampermonkey.net/
// @version       3.6
// @description   仅保留视频并居中铺满；CCTV/B站/浙江卫视独立处理，其他未指定网站复用CCTV逻辑
// @author        User
// @match         *://*/*
// @grant         GM_addStyle
// ==/UserScript==

(function () {
    'use strict';

    const hostname = location.hostname;

    // ============================================================
    // 路由区：按 hostname 分发
    // 新增特殊网站 → 在「特殊网址区」加 else if，并在后面函数区加 handleXxx()
    // 顺序：cctv → file → bilibili → 【特殊网址区】 → cztv → 兜底else(CCTV逻辑)
    // ============================================================

    // ==================== CCTV ====================
    if (hostname.includes('cctv.com') || hostname.includes('cntv.cn')) {
        runCCTVLogic();
    }

    // ==================== 本地视频文件 ====================
    else if (location.protocol === 'file:' && /\.mp4|\.webm|\.mkv/i.test(location.pathname)) {
        window.addEventListener('load', function () {
            var video = document.querySelector('video');
            if (video) {
                video.play().catch(() => {});
                setTimeout(function () {
                    (video.requestFullscreen || video.webkitRequestFullscreen || video.webkitRequestFullScreen).call(video);
                }, 500);
            }
        });
    }

    // ==================== B站 ====================
    else if (hostname.includes('bilibili.com')) {
        if (/\/video\/BV/i.test(location.pathname)) {
            console.log("[ok.js] B站视频页");

            var style = document.createElement('style');
            style.textContent = `
      .bili-header,.bili-header-m,.nav-bar,.nav-menu,
      .bpx-player-top-left,.bpx-player-top-right,
      .video-info,.up-info,.comment,.reply,.recommend,
      .video-tag,.video-title,.video-desc,.up-panel,
      footer,.footer,.page-header,
      .bpx-player-control-web,.bpx-player-sending-bar,
      .bpx-player-pc-sending-bar,.bpx-player-control-mask,
      .bpx-player-control-wrap,.bpx-player-subtitle-wrap,
      .bpx-player-danmaku-wrap {
        display: none !important;
      }
      html, body {
        margin: 0 !important; padding: 0 !important;
        overflow: hidden !important;
        width: 100% !important; height: 100% !important;
        background: #000 !important;
      }
      .bpx-player-container {
        position: fixed !important; top: 0 !important; left: 0 !important;
        width: 100vw !important; height: 100vh !important;
        z-index: 99999 !important;
        background: #000 !important;
        display: flex !important;
        align-items: center !important;
        justify-content: center !important;
      }
      .bpx-player-video-wrap,
      .bpx-player-video-area {
        background: #000 !important;
        display: flex !important;
        align-items: center !important;
        justify-content: center !important;
      }
      .bpx-player-video-area {
        width: 100vw !important;
        height: 100vh !important;
      }
      .bpx-player-video-area video,
      video {
        object-fit: contain !important;
        background: #000 !important;
      }
      .bpx-player-video-area.portrait {
        width: auto !important;
        height: 100vh !important;
        max-width: 100vw !important;
      }
      .bpx-player-video-area.portrait video {
        height: 100% !important;
        width: auto !important;
        max-width: 100vw !important;
      }
    `;
            (document.head || document.documentElement).appendChild(style);

            function adjustVideoSize() {
                var v = document.querySelector('.bpx-player-video-area video') || document.querySelector('video');
                if (!v || !v.videoWidth) return;
                var area = document.querySelector('.bpx-player-video-area');
                if (!area) return;
                var isPortrait = v.videoHeight > v.videoWidth;
                if (isPortrait) {
                    var videoRatio = v.videoWidth / v.videoHeight;
                    var screenH = window.innerHeight;
                    var targetW = screenH * videoRatio;
                    area.classList.add('portrait');
                    area.style.width = targetW + 'px';
                    area.style.height = screenH + 'px';
                } else {
                    area.classList.remove('portrait');
                    area.style.width = '100vw';
                    area.style.height = '100vh';
                }
            }

            document.addEventListener('loadedmetadata', adjustVideoSize, true);
            var _obs2 = new MutationObserver(function () {
                clearTimeout(window._adjT);
                window._adjT = setTimeout(adjustVideoSize, 200);
            });
            _obs2.observe(document.documentElement, { childList: true, subtree: true });
            window.addEventListener('resize', adjustVideoSize);

            function doFullscreen() {
                let elementNames = ["bpx-player-ctrl-web-enter", "bilibili-player-iconfont-web-fullscreen-off", "player_pagefullscreen_player", "squirtle-pagefullscreen-inactive"];
                elementNames.forEach(waitElement);
            }
            if (document.readyState === 'complete') doFullscreen();
            else window.addEventListener('load', doFullscreen, { once: true });
        }

        function waitElement(elementName) {
            var _times = 20, _interval = 1000,
                _self = document.getElementsByClassName(elementName)[0],
                _iIntervalID;
            if (_self != undefined) { _self.click(); return; }
            _iIntervalID = setInterval(function () {
                if (!_times) { clearInterval(_iIntervalID); return; }
                _times--;
                _self = document.getElementsByClassName(elementName)[0];
                if (_self == undefined) _self = document.getElementById(elementName);
                if (_self != undefined) { _self.click(); clearInterval(_iIntervalID); }
            }, _interval);
        }
    }

    // ============================================================
    // 特殊网址区：以后新增网站插这里
    // 例：
    // else if (hostname.includes('douyin.com')) { handleDouyin(); }
    // else if (hostname.includes('v.qq.com')) { handleQQ(); }
    // 对应 handleDouyin / handleQQ 函数写在文件末尾函数区
    // ============================================================

    // ==================== 浙江卫视 ====================
    else if (hostname.includes('cztv.com')) {
        handleCztv();
    }

    // ==================== 未指定网站：兜底复用 CCTV 逻辑 ====================
    else {
        runCCTVLogic();
    }

    // ============================================================
    // 函数区（全部平级，不嵌套进别的函数）
    // ============================================================

    // -------- CCTV / 兜底通用逻辑 --------
    function runCCTVLogic() {
        GM_addStyle(`
        html, body {
            margin: 0 !important; padding: 0 !important;
            background: #000 !important; overflow: hidden !important;
        }
        body.cctv-clean-mode { background: #000 !important; }
        .cctv-clean-main {
            position: fixed !important; inset: 0 !important;
            width: 100vw !important; height: 100vh !important;
            margin: 0 !important; padding: 0 !important;
            background: #000 !important; display: flex !important;
            align-items: center !important; justify-content: center !important;
            z-index: 999999 !important;
        }
        .cctv-video-center, .cctv-video-card, .cctv-video-wrap {
            width: 100% !important; height: 100% !important;
            background: #000 !important; display: flex !important;
            align-items: center !important; justify-content: center !important;
        }
        .cctv-video-wrap video, video {
            object-fit: contain !important; background: #000 !important;
        }
        body.cctv-clean-mode > *:not(.cctv-clean-main) { display: none !important; }
        `);

        function findVideoColumn() {
            const video = document.querySelector('video');
            if (!video) return null;
            let el = video.parentElement;
            const vw = window.innerWidth, vh = window.innerHeight;
            while (el && el !== document.body) {
                const r = el.getBoundingClientRect();
                if (r.width > vw * 0.3 && r.height > vh * 0.3) return el;
                el = el.parentElement;
            }
            return video.parentElement;
        }

        function findMainContainer(videoColumn) {
            if (!videoColumn) return null;
            let parent = videoColumn.parentElement;
            while (parent && parent !== document.body) {
                const kids = Array.from(parent.children).filter(c => c.offsetWidth > 0);
                if (kids.length >= 1) return parent;
                parent = parent.parentElement;
            }
            return document.body;
        }

        function applyLayout() {
            if (document.querySelector('.cctv-clean-main')) return;
            const videoColumn = findVideoColumn();
            if (!videoColumn) return;
            const main = findMainContainer(videoColumn);
            if (!main) return;

            main.classList.add('cctv-clean-main');
            videoColumn.classList.add('cctv-video-card');

            if (!videoColumn.querySelector('.cctv-video-wrap')) {
                const wrap = document.createElement('div');
                wrap.className = 'cctv-video-wrap';
                while (videoColumn.firstChild) wrap.appendChild(videoColumn.firstChild);
                videoColumn.appendChild(wrap);
            }

            const centerWrap = document.createElement('div');
            centerWrap.className = 'cctv-video-center';
            videoColumn.parentElement.insertBefore(centerWrap, videoColumn);
            centerWrap.appendChild(videoColumn);

            Array.from(main.children).forEach(child => {
                if (child !== centerWrap) child.style.display = 'none';
            });

            document.body.prepend(main);
            document.body.classList.add('cctv-clean-mode');

            const videoWrap = videoColumn.querySelector('.cctv-video-wrap');
            const fullscreenTarget = videoWrap || videoColumn;
            if (fullscreenTarget && !fullscreenTarget.dataset.fullscreenBound) {
                fullscreenTarget.dataset.fullscreenBound = '1';
                fullscreenTarget.addEventListener('dblclick', function () {
                    const el = document.fullscreenElement || document.webkitFullscreenElement;
                    if (el) { (document.exitFullscreen || document.webkitExitFullscreen).call(document); }
                    else {
                        const target = fullscreenTarget.querySelector('video') || fullscreenTarget;
                        (target.requestFullscreen || target.webkitRequestFullscreen || target.webkitRequestFullScreen).call(target);
                    }
                });
            }
        }

        function init() {
            const run = () => setTimeout(applyLayout, 800);
            if (document.readyState === 'complete') run();
            else window.addEventListener('load', run);
            const obs = new MutationObserver(() => {
                if (!document.querySelector('.cctv-clean-main') && document.querySelector('video')) applyLayout();
            });
            obs.observe(document.documentElement, { childList: true, subtree: true });
        }
        init();
    }

    // -------- 浙江卫视：只盖样式，不搬 DOM（小屏+嵌入大屏都适用） --------
    function handleCztv() {
        GM_addStyle(`
            html, body {
                margin: 0 !important; padding: 0 !important;
                background: #000 !important; overflow: hidden !important;
                width: 100% !important; height: 100% !important;
            }
            header, nav, .header, .nav, .topbar, .sidebar, .footer, footer,
            .channel-list, .program-list, .related, .recommend, .comment,
            .player-side, .player-aside,
            [class*="header"], [class*="nav-"], [class*="sidebar"], [class*="footer"],
            [class*="channelList"], [class*="programList"], [class*="recommend"] {
                display: none !important;
            }
            .cztv-player-fixed {
                position: fixed !important;
                inset: 0 !important;
                width: 100vw !important;
                height: 100vh !important;
                z-index: 999999 !important;
                background: #000 !important;
                display: flex !important;
                align-items: center !important;
                justify-content: center !important;
            }
            .cztv-player-fixed video {
                object-fit: contain !important;
                width: 100% !important;
                height: 100% !important;
                max-width: 100vw !important;
                max-height: 100vh !important;
                background: #000 !important;
            }
            .cztv-video-direct {
                position: fixed !important;
                inset: 0 !important;
                width: 100vw !important;
                height: 100vh !important;
                object-fit: contain !important;
                background: #000 !important;
                z-index: 999999 !important;
            }
        `);

        function fix() {
            const video = document.querySelector('video');
            if (!video) return;
            if (video.closest('.cztv-player-fixed, .cztv-video-direct')) return;

            // 方案A：向上找播放器容器，只加 class 不搬子节点
            let box = video.parentElement;
            while (box && box !== document.body) {
                const cls = (box.className || '').toString();
                if (/player|video|live|play|stage|container|embed|wrap/i.test(cls)) break;
                box = box.parentElement;
            }

            if (box && box !== document.body) {
                box.classList.add('cztv-player-fixed');
                const ancestors = new Set();
                let p = box;
                while (p && p !== document.body) { ancestors.add(p); p = p.parentElement; }
                Array.from(document.body.children).forEach(ch => {
                    if (!ancestors.has(ch)) ch.style.display = 'none';
                });
            } else {
                // 方案B：嵌入模式找不到容器，video 自己铺满
                video.classList.add('cztv-video-direct');
                document.body.insertBefore(video, document.body.firstChild);
                Array.from(document.body.children).forEach(ch => {
                    if (ch !== video) ch.style.display = 'none';
                });
            }

            if (!video.dataset.cztvFs) {
                video.dataset.cztvFs = '1';
                video.addEventListener('dblclick', function () {
                    const el = document.fullscreenElement || document.webkitFullscreenElement;
                    if (el) { (document.exitFullscreen || document.webkitExitFullscreen).call(document); }
                    else { (video.requestFullscreen || video.webkitRequestFullscreen || video.webkitRequestFullScreen).call(video); }
                });
            }
        }

        const run = () => setTimeout(fix, 1000);
        if (document.readyState === 'complete') run();
        else window.addEventListener('load', run);

        new MutationObserver(() => {
            const v = document.querySelector('video');
            if (v && !v.closest('.cztv-player-fixed, .cztv-video-direct')) fix();
        }).observe(document.documentElement, { childList: true, subtree: true });

        window.addEventListener('resize', fix);
    }

    // ============================================================
    // 以后新增特殊网站函数放这里，例：
    // function handleDouyin() { ... }
    // ============================================================

})();