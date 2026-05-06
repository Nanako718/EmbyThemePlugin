class Home {
	static sleep(ms) {
		return new Promise((resolve) => setTimeout(resolve, ms));
	}

	static typewriter(el, delay = 0) {
		if (!el) return;
		const text = el.dataset.title || el.textContent;
		if (!el.dataset.title) el.dataset.title = text;
		clearTimeout(el._twTimer);
		el.textContent = "";
		let i = 0;
		const speed = Math.max(26, Math.min(52, Math.ceil(2000 / text.length)));
		const tick = () => {
			if (i < text.length) {
				el.textContent += text[i++];
				el._twTimer = setTimeout(tick, speed);
			}
		};
		el._twTimer = setTimeout(tick, delay);
	}

	static shuffleArray(arr) {
		const list = arr.slice();
		for (let i = list.length - 1; i > 0; i--) {
			const j = Math.floor(Math.random() * (i + 1));
			[list[i], list[j]] = [list[j], list[i]];
		}
		return list;
	}

	static start() {
		this.initLoading();

		this.cache = { item: new Map() };
		this._sectionsObserver = null;
		this._bodyFallbackObserver = null;
		this._backdropObserver = null;
		this._homeInitPromise = null;
		this._currentItemId = null;

		document.addEventListener(
			"viewbeforeshow",
			(e) => {
				const detail = e.detail;
				if (!detail) return;
				if (detail.type !== "home") {
					if (window.MistyLoading) window.MistyLoading.fadeRemove();
					return;
				}
				const view = e.target;
				if (!view || !view.classList.contains("view")) return;

				document.querySelectorAll(".view.hide .misty-banner").forEach((el) => {
					const host = el.closest(".view");
					if (host && host !== view) el.remove();
				});

				view.setAttribute("data-type", "home");

				// 无论从哪里进入主页，只要没有 banner 就保证显示 loading
				if (!view.querySelector(".misty-banner") && !document.querySelector(".misty-loading")) {
					this.initLoading();
				}

				if (!detail.isRestored) {
					this.observeHomeSectionsContainer(view);
				} else {
					const sections = view.querySelector(".sections");
					if (!view.querySelector(".misty-banner") && sections) {
						void this.initIntoView(view, sections);
					} else if (!view.querySelector(".misty-banner")) {
						this.observeHomeSectionsContainer(view);
					}
				}
			},
			false
		);

		document.addEventListener(
			"viewbeforehide",
			(e) => {
				if (e.detail && e.detail.type === "home") {
					this.disconnectObservers();
					this.setupBodyFallbackObserver();
				}
			},
			false
		);

		this.setupBodyFallbackObserver();
	}

	static disconnectObservers() {
		if (this._sectionsObserver) {
			this._sectionsObserver.disconnect();
			this._sectionsObserver = null;
		}
		if (this._bodyFallbackObserver) {
			this._bodyFallbackObserver.disconnect();
			this._bodyFallbackObserver = null;
		}
		if (this._bannerObserver) {
			this._bannerObserver.disconnect();
			this._bannerObserver = null;
		}
		if (this._backdropObserver) {
			this._backdropObserver.disconnect();
			this._backdropObserver = null;
		}
		this._currentItemId = null;
		this.glDispose();
	}

	static setupBodyFallbackObserver() {
		if (this._bodyFallbackObserver) return;
		this._bodyFallbackObserver = new MutationObserver(() => {
			const view = document.querySelector(".view:not(.hide)");
			if (!view) return;
			if (view.querySelector(".misty-banner")) return;
			const container = view.querySelector(".homeSectionsContainer");
			if (!container) return;
			const sections = view.querySelector(".sections");
			if (!sections) return;
			this._bodyFallbackObserver.disconnect();
			this._bodyFallbackObserver = null;
			void this.initIntoView(view, sections);
		});
		this._bodyFallbackObserver.observe(document.body, {
			childList: true,
			subtree: true,
		});
	}

	static observeHomeSectionsContainer(view) {
		if (this._sectionsObserver) this._sectionsObserver.disconnect();

		// 如果 sections 已存在，直接初始化
		const existingSections = view.querySelector(".sections");
		if (existingSections && !view.querySelector(".misty-banner")) {
			void this.initIntoView(view, existingSections);
			return;
		}

		const container = view.querySelector(".homeSectionsContainer");
		// 有 container 就 observe container（无需 subtree），否则 observe view 等 container 出现
		const target = container || view;
		this._sectionsObserver = new MutationObserver(() => {
			const sections = view.querySelector(".sections");
			if (!sections || view.querySelector(".misty-banner")) return;
			this._sectionsObserver.disconnect();
			this._sectionsObserver = null;
			void this.initIntoView(view, sections);
		});
		this._sectionsObserver.observe(target, {
			childList: true,
			subtree: !container,
		});
	}

	static async initIntoView(view, sections) {
		if (view.querySelector(".misty-banner")) return;
		if (this._homeInitPromise) {
			await this._homeInitPromise;
			return;
		}
		this._homeInitPromise = (async () => {
			try {
				if (view.querySelector(".misty-banner")) return;
				await this.initBanner(view, sections);
				if (view.querySelector(".misty-banner")) this.initEvent();
			} catch (_) {
				if (window.MistyLoading) window.MistyLoading.fadeRemove();
			} finally {
				this._homeInitPromise = null;
				if (this._bodyFallbackObserver) {
					this._bodyFallbackObserver.disconnect();
					this._bodyFallbackObserver = null;
				}
			}
		})();
		await this._homeInitPromise;
	}

	static initLoading() {
		if (window.MistyLoading) window.MistyLoading.ensureDom();
	}

	static injectCode(code) {
		const seed = code + Math.random().toString() + Date.now().toString();
		let hash = "";
		if (typeof md5 === "function") {
			hash = md5(seed);
		} else {
			hash = "h" + seed.replace(/[^a-zA-Z0-9]/g, "").slice(-24);
		}
		return new Promise((resolve, reject) => {
			let settled = false;
			let cleanup = () => {};
			const done = (ok, payload) => {
				if (settled) return;
				settled = true;
				cleanup();
				ok ? resolve(payload) : reject(payload);
			};

			if ("BroadcastChannel" in window) {
				const channel = new BroadcastChannel(hash);
				const onMessage = (event) => done(true, event.data);
				channel.addEventListener("message", onMessage, { once: true });
				cleanup = () => {
					channel.removeEventListener("message", onMessage);
					channel.close();
				};
			}

			const timeoutId = setTimeout(() => done(false, new Error("injectCode timeout")), 65000);
			const prevCleanup = cleanup;
			cleanup = () => {
				clearTimeout(timeoutId);
				prevCleanup();
			};

			const script = `
			<script class="I${hash}">
				setTimeout(async ()=> {
					async function R${hash}(){${code}};
					if ("BroadcastChannel" in window) {
						const channel = new BroadcastChannel("${hash}");
						channel.postMessage(await R${hash}());
						channel.close();
					}
					document.querySelector("script.I${hash}").remove()
				}, 16)
			<\/script>
			`;
			$(document.head || document.documentElement).append(script);
		});
	}

	static injectCall(func, arg) {
		const script = `
		const client = await new Promise((resolve, reject) => {
			const t0 = Date.now();
			const id = setInterval(() => {
				if (window.ApiClient != undefined) {
					clearInterval(id);
					resolve(window.ApiClient);
				} else if (Date.now() - t0 > 60000) {
					clearInterval(id);
					reject(new Error("ApiClient timeout"));
				}
			}, 16);
		});
		return await client.${func}(${arg})
		`;
		return this.injectCode(script);
	}

	static async getItem(itemId) {
		if (this.cache.item.has(itemId)) return this.cache.item.get(itemId);
		const item = await this.injectCall("getItem", `client.getCurrentUserId(), "${itemId}"`);
		this.cache.item.set(itemId, item);
		return item;
	}

	static async initBanner(view, sections) {
		const $view = $(view);
		if ($view.find(".misty-banner").length) return;

		if (!document.querySelector(".misty-loading")) this.initLoading();

		const $banner = $(`
		<div class="misty-banner">
			<div class="misty-banner-body"></div>
			<div class="misty-banner-item">
				<div class="misty-banner-info padded-left padded-right">
					<h1></h1>
					<div><p></p></div>
					<div><button type="button" class="misty-banner-more">MORE</button></div>
				</div>
			</div>
			<div class="misty-banner-library">
				<div class="misty-banner-logos">
					<img class="misty-banner-logo" draggable="false" decoding="async" alt="Logo" src="">
				</div>
			</div>
		</div>
		`);

		const liveSection = view.querySelector(".sections") || sections;
		const insertParent = liveSection?.parentNode ?? view.querySelector(".homeSectionsContainer") ?? view;
		insertParent.insertBefore($banner[0], liveSection?.parentNode ? liveSection : null);

		this._bannerInView = true;
		this._bannerObserver = new IntersectionObserver((entries) => {
			this._bannerInView = entries[0].isIntersecting;
		}, { threshold: 0 });
		this._bannerObserver.observe($banner[0]);

		const canvas = document.createElement("canvas");
		canvas.className = "misty-banner-canvas";
		$banner.find(".misty-banner-body")[0].prepend(canvas);
		this.glInit(canvas);

		$banner.on("click", ".misty-banner-more", function (e) {
			e.preventDefault();
			e.stopPropagation();
			const id = String($(this).data("item-id") || "").replace(/[^a-zA-Z0-9-]/g, "");
			if (!id) return;
			Home.injectCode(`
				(function(){
					if (window.Emby && Emby.Page && Emby.Page.showItem) { Emby.Page.showItem("${id}"); }
					else if (window.appRouter && appRouter.showItem) { appRouter.showItem("${id}"); }
				})()
			`);
		});

		this.observeBackdrop(view);
	}

	static observeBackdrop(view) {
		const container = document.querySelector(".backdropContainer");
		if (!container) return;

		const handle = (img) => {
			if (!img?.src) return;
			const m = img.src.match(/\/Items\/(\d+)\/Images\/Backdrop/);
			if (m) this.onBackdropChange(view, m[1], img.src);
		};

		handle(container.querySelector("img.displayingBackdropImage"));

		this._backdropObserver = new MutationObserver(() => {
			handle(container.querySelector("img.displayingBackdropImage"));
		});
		this._backdropObserver.observe(container, {
			childList: true, subtree: true,
			attributes: true, attributeFilter: ["src", "class"],
		});
	}

	static async onBackdropChange(view, itemId, imgSrc) {
		if (itemId === this._currentItemId) return;
		this._currentItemId = itemId;

		const $view = $(view);
		const $item  = $view.find(".misty-banner-item");
		const $h1    = $item.find("h1");
		const $p     = $item.find("p");
		const $btn   = $item.find(".misty-banner-more");
		const $logo  = $view.find(".misty-banner-logo");

		let item;
		try { item = await this.getItem(itemId); } catch (_) { return; }

		const hdSrc = imgSrc.replace(/maxWidth=\d+/, "maxWidth=3840");
		const serverUrl = (imgSrc.match(/^(https?:\/\/[^/]+)/) || [])[1] || "";

		const fromTex  = this._glCurrentTex;
		const isFirst  = !fromTex;
		const [toTex]  = await Promise.all([
			this.glLoadTexture(hdSrc),
			isFirst ? this.sleep(3000) : Promise.resolve(),
		]);
		if (!toTex) return;

		const logoTag = item.ImageTags?.Logo;
		const logoSrc = logoTag ? `${serverUrl}/emby/Items/${itemId}/Images/Logo/0?tag=${logoTag}&maxWidth=600&quality=90` : "";

		if (isFirst) {
			// 首帧：item 还没有 active，直接更新内容再激活
			$h1.attr("data-title", item.Name || "").text("");
			$p.text(item.Overview || "");
			$btn.attr("data-item-id", itemId);
			$logo.attr("src", logoSrc);
			this.glRender(toTex);
			this._glCurrentTex = toTex;

			// canvas 入场：scale(1.06)→scale(1) + opacity 0→1
			const canvas = $view.find(".misty-banner-canvas")[0];
			if (canvas) {
				canvas.classList.add("misty-revealing");
				canvas.addEventListener("animationend", () => canvas.classList.remove("misty-revealing"), { once: true });
			}

			void $item[0].offsetHeight;
			$item.addClass("active");
			Home.typewriter($h1[0], 2800);
			clearTimeout(this.logoTimer);
			this.logoTimer = setTimeout(() => $logo.addClass("active"), 3800);
			// 稍后再撤 loading，让画面先开始动
			setTimeout(() => { if (window.MistyLoading) window.MistyLoading.fadeRemove(); }, 200);
		} else {
			// 先归零（不可见）→ 再更新内容 → 再激活，避免内容闪现
			if ($h1[0]) clearTimeout($h1[0]._twTimer);
			$item.addClass("misty-resetting");           // 瞬间归零（no transition）
			void $item[0].offsetHeight;                  // 应用归零状态
			// 此时全部不可见，安全更新内容
			$h1.attr("data-title", item.Name || "").text("");
			$p.text(item.Overview || "");
			$btn.attr("data-item-id", itemId);
			$logo.attr("src", logoSrc).removeClass("active");
			$item.removeClass("active misty-resetting"); // 回到 base（opacity:0，transition 生效）
			void $item[0].offsetHeight;                  // 稳定
			$item.addClass("active");                    // 从 0 开始，delay 按顺序触发
			Home.typewriter($h1[0], 2800);
			clearTimeout(this.logoTimer);
			this.logoTimer = setTimeout(() => $logo.addClass("active"), 3800);
			// GL blinds 过渡
			Home.glTransition(fromTex, toTex, 2500).then(() => {
				this._glCurrentTex = toTex;
				if (this._glPrevTex && this._gl) {
					this._gl.gl.deleteTexture(this._glPrevTex.tex);
				}
				this._glPrevTex = fromTex;
			});
		}
	}

	// ── WebGL Blinds Transition ──
	static _gl = null;
	static _glCurrentTex = null;
	static _glPrevTex = null;

	static glInit(canvas) {
		this.glDispose();
		const w = canvas.parentElement.clientWidth || window.innerWidth;
		const h = canvas.parentElement.clientHeight || Math.round(w * 9 / 16);
		canvas.width  = w;
		canvas.height = h;

		const gl = canvas.getContext("webgl", { preserveDrawingBuffer: true, antialias: false })
		        || canvas.getContext("experimental-webgl", { preserveDrawingBuffer: true });
		if (!gl) return;

		const compile = (type, src) => {
			const s = gl.createShader(type);
			gl.shaderSource(s, src);
			gl.compileShader(s);
			return s;
		};
		const prog = gl.createProgram();
		gl.attachShader(prog, compile(gl.VERTEX_SHADER, `
			attribute vec2 a_pos;
			varying vec2 vUv;
			void main() {
				vUv = vec2(a_pos.x * 0.5 + 0.5, 0.5 - a_pos.y * 0.5);
				gl_Position = vec4(a_pos, 0.0, 1.0);
			}
		`));
		gl.attachShader(prog, compile(gl.FRAGMENT_SHADER, `
			precision mediump float;
			varying vec2 vUv;
			uniform sampler2D tFrom;
			uniform sampler2D tTo;
			uniform float progress;
			uniform float count;
			uniform float smoothness;
			uniform float canvasAR;
			uniform float fromAR;
			uniform float toAR;

			vec2 coverUV(vec2 uv, float cAR, float tAR) {
				if (tAR > cAR) {
					float s = cAR / tAR;
					return vec2(uv.x * s + (1.0 - s) * 0.5, uv.y);
				}
				float s = tAR / cAR;
				return vec2(uv.x, uv.y * s + (1.0 - s) * 0.5);
			}

			vec4 getFromColor(vec2 p) { return texture2D(tFrom, coverUV(p, canvasAR, fromAR)); }
			vec4 getToColor(vec2 p)   { return texture2D(tTo,   coverUV(p, canvasAR, toAR));   }

			// gl-transitions: Blinds — Author: gre / License: MIT
			vec4 transition(vec2 p) {
				float pr = smoothstep(-smoothness, 0.0, p.x - progress * (1.0 + smoothness));
				float s  = step(pr, fract(count * p.x));
				return mix(getFromColor(p), getToColor(p), s);
			}

			void main() { gl_FragColor = transition(vUv); }
		`));
		gl.linkProgram(prog);
		gl.useProgram(prog);

		// full-screen quad
		const buf = gl.createBuffer();
		gl.bindBuffer(gl.ARRAY_BUFFER, buf);
		gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1,-1, 1,-1, -1,1, 1,1]), gl.STATIC_DRAW);
		const posLoc = gl.getAttribLocation(prog, "a_pos");
		gl.enableVertexAttribArray(posLoc);
		gl.vertexAttribPointer(posLoc, 2, gl.FLOAT, false, 0, 0);

		const u = (n) => gl.getUniformLocation(prog, n);
		const uniforms = {
			tFrom: u("tFrom"), tTo: u("tTo"),
			progress: u("progress"), count: u("count"), smoothness: u("smoothness"),
			canvasAR: u("canvasAR"), fromAR: u("fromAR"), toAR: u("toAR"),
		};
		gl.uniform1i(uniforms.tFrom, 0);
		gl.uniform1i(uniforms.tTo,   1);
		gl.uniform1f(uniforms.count,      10.0);
		gl.uniform1f(uniforms.smoothness, 0.5);
		gl.uniform1f(uniforms.canvasAR,   w / h);

		this._gl = { gl, uniforms };
	}

	static glLoadTexture(src) {
		return new Promise((resolve) => {
			if (!this._gl) { resolve(null); return; }
			const { gl } = this._gl;
			const tex = gl.createTexture();
			const img = new Image();
			img.onload = () => {
				gl.bindTexture(gl.TEXTURE_2D, tex);
				gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
				gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
				gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
				gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
				gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
				resolve({ tex, ar: img.naturalWidth / img.naturalHeight });
			};
			img.onerror = () => resolve(null);
			img.src = src;
		});
	}

	static glDraw(fromObj, toObj, progress) {
		if (!this._gl) return;
		const { gl, uniforms } = this._gl;
		gl.activeTexture(gl.TEXTURE0);
		gl.bindTexture(gl.TEXTURE_2D, fromObj.tex);
		gl.activeTexture(gl.TEXTURE1);
		gl.bindTexture(gl.TEXTURE_2D, toObj.tex);
		gl.uniform1f(uniforms.fromAR,   fromObj.ar);
		gl.uniform1f(uniforms.toAR,     toObj.ar);
		gl.uniform1f(uniforms.progress, progress);
		gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
	}

	static glRender(texObj) {
		if (texObj) this.glDraw(texObj, texObj, 0);
	}

	static glTransition(fromObj, toObj, duration = 1200) {
		return new Promise((resolve) => {
			if (!this._gl || !fromObj || !toObj) { resolve(); return; }
			let elapsed = 0;
			let lastTs = performance.now();
			const frame = () => {
				const now = performance.now();
				if (Home._bannerInView !== false && !document.hidden) {
					elapsed += now - lastTs;
				}
				lastTs = now;
				const p = Math.min(elapsed / duration, 1.0);
				this.glDraw(fromObj, toObj, p);
				if (p < 1.0) requestAnimationFrame(frame);
				else resolve();
			};
			requestAnimationFrame(frame);
		});
	}

	static glDispose() {
		if (!this._gl) return;
		const { gl } = this._gl;
		[this._glCurrentTex, this._glPrevTex].forEach((t) => t && gl.deleteTexture(t.tex));
		this._gl = null;
		this._glCurrentTex = null;
		this._glPrevTex = null;
	}

	static initEvent() {
		const script = `
		if (!window.appRouter) {
			try { window.appRouter = (await window.require(["appRouter"]))[0]; } catch (e) {}
		}
		`;
		this.injectCode(script);
	}
}

if ("BroadcastChannel" in window) {
	(function waitForJQuery() {
		if (typeof $ === "undefined") { setTimeout(waitForJQuery, 16); return; }
		if ($("meta[name=application-name]").attr("content") == "Emby" || $(".accent-emby") != undefined) {
			Home.start();
		}
	})();
}
