// The web version's page glue: the start screen, the frame loop, saves kept in IndexedDB, sound unlocking
// and the few browser calls the game makes (Peggle.Web/WebHost.cs, WebSaves.cs, Pages/Index.razor.cs).
(function () {
    'use strict';

    // Browsers only let a page make sound after the player has interacted with it. Every AudioContext (KNI
    // makes its own) is remembered and resumed on each click, tap or key press, so sound starts with the first
    // one, wherever it was.
    const audioContexts = [];
    const NativeAudioContext = window.AudioContext || window.webkitAudioContext;
    if (NativeAudioContext) {
        class TrackedAudioContext extends NativeAudioContext {
            constructor(...args) {
                super(...args);
                audioContexts.push(this);
            }
        }
        window.AudioContext = TrackedAudioContext;
        if (window.webkitAudioContext) {
            window.webkitAudioContext = TrackedAudioContext;
        }
    }
    function resumeAudio() {
        for (const context of audioContexts) {
            if (context.state === 'suspended' || context.state === 'interrupted') {
                context.resume().catch(() => { });
            }
        }
    }
    for (const type of ['pointerdown', 'pointerup', 'touchend', 'mousedown', 'keydown']) {
        window.addEventListener(type, resumeAudio, true);
    }

    // Keep the page itself from scrolling or zooming (arrow keys, space, the wheel), e.g. when embedded.
    window.addEventListener('keydown', (event) => {
        if ([' ', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'].includes(event.key)) {
            event.preventDefault();
        }
    });
    window.addEventListener('wheel', (event) => event.preventDefault(), { passive: false });
    window.addEventListener('contextmenu', (event) => event.preventDefault());

    // Saves: one zip per edition, shared by the release and the pre-release of that edition, and kept apart
    // from other sites on the same origin (github.io pages share one) by the site's first path segment.
    function savesKey() {
        const site = location.pathname.split('/').filter((part) => part.length > 0)[0] || '';
        return 'saves|' + site + '|' + document.documentElement.dataset.edition;
    }

    let dbPromise = null;
    function openDb() {
        if (!dbPromise) {
            dbPromise = new Promise((resolve, reject) => {
                const request = indexedDB.open('peggle-enhanced', 1);
                request.onupgradeneeded = () => request.result.createObjectStore('saves');
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
        }
        return dbPromise;
    }

    let instance = null;
    let running = false;

    function tick() {
        if (!running) {
            return;
        }
        try {
            instance.invokeMethod('TickDotNet');
        } catch (e) {
            running = false;
            console.error(e);
            return;
        }
        window.requestAnimationFrame(tick);
    }

    function flushSaves() {
        if (instance && running) {
            try {
                instance.invokeMethod('FlushSaves');
            } catch (e) {
                console.error(e);
            }
        }
    }
    document.addEventListener('visibilitychange', () => {
        if (document.visibilityState === 'hidden') {
            flushSaves();
        }
    });
    window.addEventListener('pagehide', flushSaves);

    function start() {
        document.getElementById('start').remove();
        resumeAudio();
        // Asks the browser not to clear the saves when it runs low on space.
        if (navigator.storage && navigator.storage.persist) {
            navigator.storage.persist().catch(() => { });
        }
        const canvas = document.getElementById('theCanvas');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;
        running = true;
        window.requestAnimationFrame(tick);
    }

    window.peggleWeb = {
        loadSaves: async function () {
            try {
                const db = await openDb();
                return await new Promise((resolve) => {
                    const request = db.transaction('saves', 'readonly').objectStore('saves').get(savesKey());
                    request.onsuccess = () => resolve(request.result ? new Uint8Array(request.result) : null);
                    request.onerror = () => resolve(null);
                });
            } catch (e) {
                console.warn('Saves unavailable (private browsing?):', e);
                return null;
            }
        },

        storeSaves: function (bytes) {
            const data = new Uint8Array(bytes);
            openDb().then((db) => {
                db.transaction('saves', 'readwrite').objectStore('saves').put(data, savesKey());
            }).catch((e) => console.warn('Could not store the saves:', e));
        },

        // The game is loaded: the Play button starts it (the click also lets the game play sound).
        ready: function (dotNetInstance) {
            instance = dotNetInstance;
            document.getElementById('start-loading').remove();
            const button = document.getElementById('start-button');
            button.hidden = false;
            button.addEventListener('click', start, { once: true });
        },

        isTouch: () => window.matchMedia('(pointer: coarse)').matches,

        canvasSize: () => [window.innerWidth, window.innerHeight],

        prompt: (text) => window.prompt('', text),

        openUrl: function (url) {
            const opened = window.open(url, '_blank');
            if (opened) {
                opened.opener = null;
            } else {
                // A blocked popup: leave for the link instead (the saves are already stored).
                flushSaves();
                location.assign(url);
            }
        },

        reload: () => location.reload()
    };
})();
