// ==UserScript==
// @name          CCTV节目过滤脚本（精简版·仅视频）
// @namespace     http://tampermonkey.net/
// @version       3.0
// @description   仅保留左侧视频并居中，多网站支持
// @author        User
// @match         https://tv.cctv.com/live/*
// @match         https://www.bilibili.com/video/*
// @grant         GM_addStyle
// ==/UserScript==

(function () {
    'use strict';

    const hostname = location.hostname;

    // ==================== CCTV ====================
    if (hostname.includes('cctv.com')) {

GM_addStyle(`
  html, body {
    margin: 0 !important;
    padding: 0 !important;
    background: #000 !important;
    overflow: hidden !important;
  }

  body.cctv-clean-mode {
    background: #000 !important;
  }

  .cctv-clean-main {
    position: fixed !important;
    inset: 0 !important;
    width: 100vw !important;
    height: 100vh !important;
    margin: 0 !important;
    padding: 0 !important;
    background: #000 !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    z-index: 999999 !important;
    box-shadow: none !important;
    border: none !important;
  }

  .cctv-video-center {
    width: 100% !important;
    height: 100% !important;
    background: #000 !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
  }

  .cctv-video-card {
    background: #000 !important;
    box-shadow: none !important;
    border: none !important;
    border-radius: 0 !important;
    padding: 0 !important;
    margin: 0 !important;
    width: 100% !important;
    height: 100% !important;
    max-width: 100vw !important;
    max-height: 100vh !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
  }

  .cctv-video-wrap {
    width: 100% !important;
    height: 100% !important;
    max-width: 100vw !important;
    max-height: 100vh !important;
    background: #000 !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    aspect-ratio: 16 / 9;
  }

  .cctv-video-wrap video,
  video {
    background: #000 !important;
    object-fit: contain !important;
  }

  body.cctv-clean-mode > *:not(.cctv-clean-main) {
    display: none !important;
  }
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
                if (el) {
                    (document.exitFullscreen || document.webkitExitFullscreen).call(document);
                } else {
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

// ==================== 本地视频 ====================
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
        console.log("[ok.js] B站视频页 - 全屏铺满模式");

        // 1) CSS 铺满（隐藏多余区域 + 视频撑满）
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
            }
            .bpx-player-video-wrap,.bpx-player-video-area {
                width: 100% !important; height: 100% !important;
            }
            video {
                width: 100% !important; height: 100% !important;
                object-fit: cover !important;
            }
        `;
        (document.head || document.documentElement).appendChild(style);

        // 2) 点全屏按钮（触发 B站音频激活）
        function doFullscreen() {
            let elementNames = ["bpx-player-ctrl-web-enter", "bilibili-player-iconfont-web-fullscreen-off", "player_pagefullscreen_player", "squirtle-pagefullscreen-inactive"];
            for (var i = 0; i < elementNames.length; i++) {
                waitElement(elementNames[i]);
            }
        }

        if (document.readyState === 'complete') {
            doFullscreen();
        } else {
            window.addEventListener('load', doFullscreen, { once: true });
        }
    }

    function waitElement(elementName) {
        var _times = 20,
            _interval = 1000,
            _self = document.getElementsByClassName(elementName)[0],
            _iIntervalID;
        if (_self != undefined) {
            _self.click();
        } else {
            _iIntervalID = setInterval(function () {
                if (!_times) { clearInterval(_iIntervalID); }
                _times <= 0 || _times--;
                _self = document.getElementsByClassName(elementName)[0];
                if (_self == undefined) { _self = document.getElementById(elementName); }
                if (_self != undefined) {
                    _self.click();
                    clearInterval(_iIntervalID);
                }
            }, _interval);
        }
    }
}

    // ==================== 抖音（自行折腾） ====================
    // else if (hostname.includes('douyin.com')) {
    //     // 抖音代码
    // }

    // ==================== 快手（自行折腾） ====================
    // else if (hostname.includes('kuaishou.com')) {
    //     // 快手代码原
    // }

})();