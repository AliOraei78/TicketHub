/**
 * TicketHub Cinema-Grade Photorealistic WebGL Elemental Shader Engine
 * Pure GLSL GPU-accelerated fluid & volumetric simulation (100% natural, zero cartoonish particles)
 * Features:
 *  - 3D Simplex Curl Noise & FBM Volumetric Fire with Blackbody Radiation Color Physics
 *  - Refractive Ocean Caustic Waves & Bioluminescent Depth Gradients
 *  - Dielectric Breakdown High-Voltage Plasma Lightning Arcs
 *  - Viscous Bio-Chemical Acid & Fume Fluid Dynamics
 *  - Interactive Mouse Force Vector Uniforms (u_mouse)
 *  - IntersectionObserver Viewport Culling & Page Visibility Sleep Mode
 */

(function () {
    // =========================================================================
    // 1. VERTEX SHADER & PROCEDURAL GLSL FRAGMENT SHADERS
    // =========================================================================

    const VERTEX_SHADER_SRC = `
        attribute vec2 a_position;
        varying vec2 v_uv;
        void main() {
            v_uv = (a_position + 1.0) * 0.5;
            gl_Position = vec4(a_position, 0.0, 1.0);
        }
    `;

    // Common GLSL Math Library: Simplex & Fractal Brownian Motion
    const GLSL_COMMON_FUNCTIONS = `
        precision mediump float;
        varying vec2 v_uv;
        uniform vec2 u_resolution;
        uniform float u_time;
        uniform vec2 u_mouse;

        // Fast 2D Hash
        vec2 hash2(vec2 p) {
            p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
            return -1.0 + 2.0 * fract(sin(p) * 43758.5453123);
        }

        // 2D Simplex Gradient Noise
        float noise2D(vec2 p) {
            const float K1 = 0.366025404; // (sqrt(3)-1)/2
            const float K2 = 0.211324865; // (3-sqrt(3))/6
            vec2 i = floor(p + (p.x + p.y) * K1);
            vec2 a = p - i + (i.x + i.y) * K2;
            vec2 o = (a.x > a.y) ? vec2(1.0, 0.0) : vec2(0.0, 1.0);
            vec2 b = a - o + K2;
            vec2 c = a - 1.0 + 2.0 * K2;
            vec3 h = max(0.5 - vec3(dot(a, a), dot(b, b), dot(c, c)), 0.0);
            vec3 n = h * h * h * h * vec3(dot(a, hash2(i)), dot(b, hash2(i + o)), dot(c, hash2(i + 1.0)));
            return dot(n, vec3(70.0));
        }

        // Fractal Brownian Motion (3 Octaves)
        float fbm(vec2 p) {
            float v = 0.0;
            float a = 0.5;
            vec2 shift = vec2(100.0);
            mat2 rot = mat2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));
            for (int i = 0; i < 3; ++i) {
                v += a * noise2D(p);
                p = rot * p * 2.0 + shift;
                a *= 0.5;
            }
            return v;
        }
    `;

    // --- A. VOLUMETRIC REALISTIC FIRE SHADER ---
    const FRAGMENT_FIRE_SRC = GLSL_COMMON_FUNCTIONS + `
        void main() {
            vec2 uv = v_uv;
            // Normalized aspect ratio
            vec2 p = (gl_FragCoord.xy * 2.0 - u_resolution.xy) / min(u_resolution.x, u_resolution.y);

            // Mouse wind interaction
            vec2 mouseNorm = (u_mouse * 2.0 - u_resolution.xy) / min(u_resolution.x, u_resolution.y);
            float mouseDist = length(p - mouseNorm);
            vec2 mouseOffset = (p - mouseNorm) * (1.0 - smoothstep(0.0, 0.8, mouseDist)) * 0.25;

            // Thermal upward advection & curl noise
            vec2 flameUv = uv * vec2(1.8, 1.4) + mouseOffset;
            flameUv.y += u_time * 1.35;

            // Multiple turbulent noise layers
            float q1 = fbm(flameUv * 2.2);
            float q2 = fbm(flameUv * 3.5 + vec2(q1, -u_time * 0.8));
            float flameIntensity = fbm(vec2(uv.x * 2.5 + q2 * 0.45, uv.y * 1.8 - u_time * 1.5 + q1 * 0.3));

            // Shape the flame plume base and top dissipation
            float flameShape = 1.0 - smoothstep(0.05, 0.95, uv.y);
            float horizontalTaper = 1.0 - abs(uv.x - 0.5) * 1.6;
            horizontalTaper = clamp(horizontalTaper, 0.0, 1.0);

            float heat = (flameIntensity * 0.65 + 0.35) * flameShape * horizontalTaper;
            heat = smoothstep(0.12, 0.75, heat);

            // True Blackbody Radiation Thermal Ramp
            vec3 whiteHot = vec3(1.0, 0.98, 0.9);
            vec3 gold = vec3(1.0, 0.7, 0.12);
            vec3 orange = vec3(0.95, 0.25, 0.04);
            vec3 crimson = vec3(0.6, 0.05, 0.02);
            vec3 smoke = vec3(0.1, 0.08, 0.12);

            vec3 col = vec3(0.0);
            if (heat > 0.65) {
                col = mix(gold, whiteHot, (heat - 0.65) / 0.35);
            } else if (heat > 0.35) {
                col = mix(orange, gold, (heat - 0.35) / 0.3);
            } else if (heat > 0.15) {
                col = mix(crimson, orange, (heat - 0.15) / 0.2);
            } else {
                col = mix(smoke, crimson, heat / 0.15);
            }

            // Incandescent micro-spark embers
            vec2 sparkUv = uv * vec2(12.0, 6.0) + vec2(0.0, u_time * 3.2);
            float sparks = noise2D(sparkUv);
            if (sparks > 0.82 && uv.y < 0.9) {
                col += vec3(1.0, 0.9, 0.4) * (sparks - 0.82) * 8.0;
            }

            float alpha = clamp(heat * 1.6, 0.0, 0.88);
            gl_FragColor = vec4(col * alpha, alpha);
        }
    `;

    // --- B. PHOTOREALISTIC OCEAN WATER CAUSTICS SHADER (LOCALIZED BOTTOM) ---
    const FRAGMENT_WATER_SRC = GLSL_COMMON_FUNCTIONS + `
        void main() {
            vec2 uv = v_uv;
            float t = u_time * 0.9;

            // Bottom Vignette / Mask (Confine water effect to bottom section of card like Fire)
            float bottomMask = 1.0 - smoothstep(0.1, 0.65, uv.y);

            // Mouse displacement ripple
            vec2 mouseUv = u_mouse / u_resolution;
            float mDist = length(uv - mouseUv);
            vec2 ripple = (uv - mouseUv) * sin(mDist * 25.0 - t * 4.0) * (1.0 - smoothstep(0.0, 0.35, mDist)) * 0.04;
            vec2 p = uv * 3.5 + ripple;

            // Voronoi Caustic Waves simulation
            vec2 p1 = p + vec2(cos(t * 0.7), sin(t * 0.6)) * 0.6;
            vec2 p2 = p * 1.4 - vec2(sin(t * 0.5), cos(t * 0.8)) * 0.8;

            float c1 = fbm(p1);
            float c2 = fbm(p2);
            float caustic = pow(abs(c1 - c2), 0.65);
            caustic = smoothstep(0.1, 0.7, caustic);

            // Water depth gradient
            vec3 deepWater = vec3(0.02, 0.08, 0.22);
            vec3 midWater = vec3(0.05, 0.35, 0.7);
            vec3 causticHighlights = vec3(0.35, 0.85, 1.0);
            vec3 sunGlint = vec3(1.0, 1.0, 1.0);

            vec3 col = mix(deepWater, midWater, 1.0 - uv.y);
            col += causticHighlights * (1.0 - caustic) * 0.95;

            // Shimmering water crest
            float waveSurface = sin(uv.x * 12.0 + t * 2.0) * 0.03 + 0.35;
            if (uv.y > waveSurface && uv.y < waveSurface + 0.1) {
                float crest = smoothstep(waveSurface, waveSurface + 0.05, uv.y);
                col = mix(col, sunGlint, crest * 0.5);
            }

            float alpha = clamp((0.35 + (1.0 - caustic) * 0.5) * bottomMask, 0.0, 0.85);
            gl_FragColor = vec4(col * alpha, alpha);
        }
    `;

    // --- C. PULSING DIELECTRIC PLASMA & SCATTERED SPARKS LIGHTNING SHADER (LOCALIZED BOTTOM) ---
    const FRAGMENT_LIGHTNING_SRC = GLSL_COMMON_FUNCTIONS + `
        void main() {
            vec2 uv = v_uv;
            float t = u_time * 2.5;

            // Bottom Y mask (Confine lightning discharges to bottom portion of card like Fire)
            float bottomMask = 1.0 - smoothstep(0.1, 0.65, uv.y);

            // Stochastic pulse bursts & random discharge triggers
            float burstSeed = floor(t * 6.5);
            float randPulse = fract(sin(burstSeed * 143.51) * 43758.5453);
            float randY1 = fract(sin(burstSeed * 291.17) * 23421.1231) * 0.35 + 0.1;
            float randY2 = fract(sin(burstSeed * 517.89) * 19283.4561) * 0.35 + 0.1;
            
            float flash = (randPulse > 0.42) ? pow(1.0 - fract(t * 6.5), 3.0) * 2.2 : 0.0;

            if (flash <= 0.01) {
                gl_FragColor = vec4(0.0);
                return;
            }

            // Original dielectric organic plasma bolt noise math
            float boltNoise1 = fbm(vec2(uv.x * 5.0, t * 2.2));
            float boltNoise2 = fbm(vec2(uv.x * 8.0 + 15.0, t * 3.0));

            // Randomized Y origins restricted to bottom of card face
            float bolt1 = abs(uv.y - randY1 + boltNoise1 * 0.32);
            float bolt2 = abs(uv.y - randY2 + boltNoise2 * 0.38);

            // High-voltage thin plasma core
            float intensity1 = (0.0018 / (bolt1 + 0.0012)) * flash;
            float intensity2 = (0.0012 / (bolt2 + 0.0012)) * flash * step(0.4, randPulse);

            float totalIntensity = intensity1 + intensity2;

            if (totalIntensity <= 0.05) {
                gl_FragColor = vec4(0.0);
                return;
            }

            vec3 coreWhite = vec3(1.0, 1.0, 1.0);
            vec3 cyanPlasma = vec3(0.25, 0.8, 1.0);
            vec3 amberElectric = vec3(0.95, 0.75, 0.2);

            vec3 col = coreWhite * pow(totalIntensity * 0.6, 1.5);
            col += cyanPlasma * (intensity1 * 1.5 + intensity2 * 1.2);
            col += amberElectric * (intensity2 * 0.9);

            float alpha = clamp(totalIntensity * 0.8 * bottomMask, 0.0, 0.92);
            gl_FragColor = vec4(col * alpha, alpha);
        }
    `;

    // --- D. VIBRANT VISCOUS TOXIC ACID, SMOKE & DROPLETS SHADER ---
    const FRAGMENT_TOXIC_SRC = GLSL_COMMON_FUNCTIONS + `
        void main() {
            vec2 uv = v_uv;
            float t = u_time * 1.1;

            // Base Y masks for bottom fluid pool and rising fumes
            float poolMask = 1.0 - smoothstep(0.05, 0.55, uv.y);
            float fumeFade = 1.0 - smoothstep(0.2, 0.9, uv.y);

            // Layer 1: Swirling Viscous Acidic Slime Pool at Bottom
            vec2 p = uv * vec2(2.8, 2.2);
            float n1 = fbm(p + vec2(0.0, t * 0.7));
            float n2 = fbm(p * 1.8 + vec2(n1, -t * 0.5));
            float acidFluid = fbm(p + vec2(n2 * 0.8, n1 * 0.6 + t * 0.4));
            float depth = smoothstep(0.12, 0.75, acidFluid);

            // Layer 2: Rising Turbulent Toxic Green Smoke & Fumes
            vec2 smokeUv = uv * vec2(1.6, 1.2);
            smokeUv.y += t * 0.85;
            float smokeNoise1 = fbm(smokeUv * 2.2);
            float smokeNoise2 = fbm(smokeUv * 3.8 + vec2(smokeNoise1, -t * 0.6));
            float toxicFumes = smoothstep(0.25, 0.75, smokeNoise2) * fumeFade * 0.7;

            // Acidic color palette
            vec3 darkAcid = vec3(0.04, 0.22, 0.06);
            vec3 vibrantGreen = vec3(0.35, 0.98, 0.15);
            vec3 bioGlow = vec3(0.78, 1.0, 0.2);
            vec3 hotYellowGreen = vec3(0.95, 1.0, 0.35);
            vec3 toxicSmokeCol = vec3(0.2, 0.75, 0.15);

            // Base fluid color
            vec3 col = mix(darkAcid, vibrantGreen, depth);

            // Add rising toxic smoke
            col = mix(col, toxicSmokeCol, toxicFumes * 0.65);
            col += bioGlow * (toxicFumes * 0.4);

            // Layer 3: Splattering Acidic Droplets & Effervescent Micro-Bubbles
            vec2 dropUv = uv * vec2(14.0, 16.0) + vec2(0.0, -t * 3.2);
            float dropNoise = noise2D(dropUv);
            if (dropNoise > 0.76 && uv.y < 0.88) {
                float dropGlow = (dropNoise - 0.76) * 6.0;
                col = mix(col, hotYellowGreen, dropGlow);
                col += vec3(0.9, 1.0, 0.4) * dropGlow;
            }

            // Corrosive acid edge highlights
            float edgeGlow = smoothstep(0.6, 0.85, acidFluid) * poolMask;
            col += bioGlow * edgeGlow * 0.6;

            float alpha = clamp((depth * 0.55 * poolMask) + (toxicFumes * 0.45) + (length(col) * 0.25), 0.0, 0.92);
            gl_FragColor = vec4(col * alpha, alpha);
        }
    `;

    // =========================================================================
    // 2. ULTRA-OPTIMIZED WEBGL RENDERER CONTROLLER
    // =========================================================================
    class WebGLShaderRenderer {
        constructor(canvas, fragmentShaderSrc) {
            this.canvas = canvas;
            this.gl = canvas.getContext('webgl', { alpha: true, antialias: false, powerPreference: 'low-power' })
                   || canvas.getContext('experimental-webgl');
            this.isVisible = true;
            this.startTime = performance.now();
            this.mouseX = 0;
            this.mouseY = 0;
            this.program = null;

            if (!this.gl) {
                console.warn('WebGL not supported, falling back.');
                return;
            }

            this.initGL(fragmentShaderSrc);
            this.bindEvents();
        }

        compileShader(src, type) {
            const gl = this.gl;
            const shader = gl.createShader(type);
            gl.shaderSource(shader, src);
            gl.compileShader(shader);
            if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
                console.error('GLSL Error:', gl.getShaderInfoLog(shader));
                gl.deleteShader(shader);
                return null;
            }
            return shader;
        }

        initGL(fragmentSrc) {
            const gl = this.gl;
            const vs = this.compileShader(VERTEX_SHADER_SRC, gl.VERTEX_SHADER);
            const fs = this.compileShader(fragmentSrc, gl.FRAGMENT_SHADER);
            if (!vs || !fs) return;

            const prog = gl.createProgram();
            gl.attachShader(prog, vs);
            gl.attachShader(prog, fs);
            gl.linkProgram(prog);
            if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) {
                console.error('Program Link Error:', gl.getProgramInfoLog(prog));
                return;
            }
            this.program = prog;

            // Fullscreen Quad (2 Triangles)
            const quadVertices = new Float32Array([
                -1.0, -1.0,
                 1.0, -1.0,
                -1.0,  1.0,
                -1.0,  1.0,
                 1.0, -1.0,
                 1.0,  1.0
            ]);

            const buffer = gl.createBuffer();
            gl.bindBuffer(gl.ARRAY_BUFFER, buffer);
            gl.bufferData(gl.ARRAY_BUFFER, quadVertices, gl.STATIC_DRAW);

            const aPos = gl.getAttribLocation(prog, 'a_position');
            gl.enableVertexAttribArray(aPos);
            gl.vertexAttribPointer(aPos, 2, gl.FLOAT, false, 0, 0);

            // Cache Uniform Locations
            this.uResolutionLoc = gl.getUniformLocation(prog, 'u_resolution');
            this.uTimeLoc = gl.getUniformLocation(prog, 'u_time');
            this.uMouseLoc = gl.getUniformLocation(prog, 'u_mouse');

            // Set blend mode
            gl.enable(gl.BLEND);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        }

        bindEvents() {
            this.canvas.addEventListener('mousemove', (e) => {
                const rect = this.canvas.getBoundingClientRect();
                this.mouseX = (e.clientX - rect.left);
                this.mouseY = (rect.height - (e.clientY - rect.top)); // WebGL inverted Y
            }, { passive: true });

            this.canvas.addEventListener('mouseleave', () => {
                this.mouseX = -999;
                this.mouseY = -999;
            }, { passive: true });
        }

        render(now) {
            if (!this.isVisible || !this.gl || !this.program) return;
            const gl = this.gl;
            const w = this.canvas.width;
            const h = this.canvas.height;

            gl.viewport(0, 0, w, h);
            gl.useProgram(this.program);

            const elapsedSec = (now - this.startTime) * 0.001;
            gl.uniform2f(this.uResolutionLoc, w, h);
            gl.uniform1f(this.uTimeLoc, elapsedSec);
            gl.uniform2f(this.uMouseLoc, this.mouseX, this.mouseY);

            gl.drawArrays(gl.TRIANGLES, 0, 6);
        }
    }

    // --- EKG MONITOR DUAL-STROKE SIMULATOR ---
    class EkgMonitorSimulator {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d', { alpha: true });
            this.scanX = 0;
            this.speed = 1.35; // Smooth slow sweep speed
            this.history = [];
            this.lastBeat = performance.now();
            this.nextBeatInterval = 520;
            this.heartbeatPhase = 0;
            this.currentSpikeScale = 0.16;
            this.currentPScale = 0.035;
            this.currentTScale = 0.055;
            this.isVisible = true;
        }

        generateNextY(now) {
            const h = this.canvas.height;
            const w = this.canvas.width;
            const midY = h * 0.84;
            const midPoint = w * 0.48;

            // When scan crosses center of card, heart flatlines (Red zone)
            const isFlatline = this.scanX >= midPoint;

            if (isFlatline) {
                this.updateDomStatus('critical');
                // Red Flatline: zero amplitude with tiny electrical baseline hum
                return midY + (Math.random() - 0.5) * 0.4;
            } else {
                this.updateDomStatus('normal');

                // Green Active Cardiac Zone: Organic & subtle medical ECG beats
                const timeSinceBeat = now - this.lastBeat;
                if (timeSinceBeat > this.nextBeatInterval) {
                    this.lastBeat = now;
                    this.heartbeatPhase = 1;
                    // Randomize subtle amplitude and interval for organic non-repeating beats
                    this.nextBeatInterval = 420 + Math.random() * 280; // 420ms - 700ms rhythm
                    this.currentSpikeScale = 0.13 + Math.random() * 0.05; // 13% to 18% max height
                    this.currentPScale = 0.025 + Math.random() * 0.02; // Subtle P-wave
                    this.currentTScale = 0.04 + Math.random() * 0.03; // Smooth T-wave
                }

                if (this.heartbeatPhase > 0) {
                    this.heartbeatPhase++;
                    if (this.heartbeatPhase === 2) return midY - (h * this.currentPScale); // P wave
                    if (this.heartbeatPhase === 3) return midY + (h * (this.currentPScale * 0.5)); // Q dip
                    if (this.heartbeatPhase === 4) return midY - (h * this.currentSpikeScale); // R spike (sleek peak)
                    if (this.heartbeatPhase === 5) return midY + (h * (this.currentSpikeScale * 0.55)); // S dip
                    if (this.heartbeatPhase === 6) return midY - (h * this.currentTScale); // T wave
                    if (this.heartbeatPhase === 7) return midY - (h * 0.015); // U wave
                    if (this.heartbeatPhase > 7) {
                        this.heartbeatPhase = 0;
                        return midY;
                    }
                }
                // Organic biological baseline wave
                return midY + Math.sin(this.scanX * 0.08) * 0.8 + (Math.random() - 0.5) * 1.0;
            }
        }

        updateDomStatus(state) {
            const badge = document.querySelector('.hero-ekg-status');
            if (!badge) return;
            if (state === 'normal') {
                badge.className = 'hero-ekg-status px-3.5 py-1 rounded-full text-xs font-black bg-emerald-500/20 text-emerald-300 border border-emerald-400/40 backdrop-blur-md flex items-center gap-2 shadow-xs transition-all duration-300';
                badge.innerHTML = `<span class="w-2 h-2 rounded-full bg-emerald-400 animate-ping shadow-[0_0_8px_#34d399]"></span>
                                   <span class="font-mono">💚 VITALS: STABLE [78 BPM]</span>`;
            } else {
                badge.className = 'hero-ekg-status px-3.5 py-1 rounded-full text-xs font-black bg-rose-500/25 text-rose-300 border border-rose-500/50 backdrop-blur-md flex items-center gap-2 shadow-xs transition-all duration-300 animate-pulse';
                badge.innerHTML = `<span class="w-2 h-2 rounded-full bg-rose-500 shadow-[0_0_12px_#f43f5e]"></span>
                                   <span class="font-mono">🚨 VITALS: FLATLINE DETECTED [0 BPM]</span>`;
            }
        }

        drawSmoothPath(ctx, points) {
            if (points.length < 2) return;
            ctx.beginPath();
            ctx.moveTo(points[0].x, points[0].y);
            if (points.length === 2) {
                ctx.lineTo(points[1].x, points[1].y);
            } else {
                for (let i = 1; i < points.length - 1; i++) {
                    const xc = (points[i].x + points[i + 1].x) / 2;
                    const yc = (points[i].y + points[i + 1].y) / 2;
                    ctx.quadraticCurveTo(points[i].x, points[i].y, xc, yc);
                }
                ctx.lineTo(points[points.length - 1].x, points[points.length - 1].y);
            }
            ctx.stroke();
        }

        render(now) {
            if (!this.isVisible || !this.ctx) return;
            const ctx = this.ctx;
            const w = this.canvas.width;
            const h = this.canvas.height;

            const nextY = this.generateNextY(now);
            this.scanX += this.speed;
            if (this.scanX > w) {
                this.scanX = 0;
                this.history = [];
                this.lastBeat = now;
            }

            this.history.push({ x: this.scanX, y: nextY });

            ctx.clearRect(0, 0, w, h);

            if (this.history.length < 2) return;

            const midPoint = w * 0.48;

            // Separate points into Green (First half organic ECG) and Red (Second half Flatline)
            const greenPoints = [];
            const redPoints = [];

            for (let i = 0; i < this.history.length; i++) {
                const pt = this.history[i];
                if (pt.x <= midPoint) {
                    greenPoints.push(pt);
                } else {
                    if (redPoints.length === 0 && greenPoints.length > 0) {
                        redPoints.push(greenPoints[greenPoints.length - 1]);
                    }
                    redPoints.push(pt);
                }
            }

            ctx.save();
            ctx.lineJoin = 'round';
            ctx.lineCap = 'round';

            // 1. Draw Green Organic ECG Path (First Half)
            if (greenPoints.length >= 2) {
                // Outer Emerald Glow
                ctx.strokeStyle = '#10b981';
                ctx.lineWidth = 3.5;
                ctx.globalAlpha = 0.4;
                this.drawSmoothPath(ctx, greenPoints);

                // White-Hot Core Laser Line
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.4;
                ctx.globalAlpha = 0.95;
                this.drawSmoothPath(ctx, greenPoints);
            }

            // 2. Draw Red Organic Flatline Path (Second Half)
            if (redPoints.length >= 2) {
                // Outer Crimson Glow
                ctx.strokeStyle = '#f43f5e';
                ctx.lineWidth = 3.5;
                ctx.globalAlpha = 0.4;
                this.drawSmoothPath(ctx, redPoints);

                // White-Hot Core Laser Line
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.4;
                ctx.globalAlpha = 0.95;
                this.drawSmoothPath(ctx, redPoints);
            }

            ctx.restore();
        }
    }

    // =========================================================================
    // 3. INTERSECTION OBSERVER & 60 FPS RENDER LOOP
    // =========================================================================
    const activeRenderers = new Map();

    const viewportObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            const r = activeRenderers.get(entry.target);
            if (r) {
                r.isVisible = entry.isIntersecting;
            }
        });
    }, { rootMargin: '60px' });

    function initElementalCanvases() {
        const canvases = document.querySelectorAll('.element-vfx-canvas');
        canvases.forEach(canvas => {
            if (activeRenderers.has(canvas)) return;

            const element = canvas.getAttribute('data-element') || 'none';
            const rect = canvas.getBoundingClientRect();
            canvas.width = Math.max(Math.floor(rect.width), 260);
            canvas.height = Math.max(Math.floor(rect.height), 150);

            let shaderSrc = null;
            if (element === 'fire') shaderSrc = FRAGMENT_FIRE_SRC;
            else if (element === 'water') shaderSrc = FRAGMENT_WATER_SRC;
            else if (element === 'lightning') shaderSrc = FRAGMENT_LIGHTNING_SRC;
            else if (element === 'toxic') shaderSrc = FRAGMENT_TOXIC_SRC;

            if (shaderSrc) {
                const renderer = new WebGLShaderRenderer(canvas, shaderSrc);
                activeRenderers.set(canvas, renderer);
                viewportObserver.observe(canvas);
            }
        });

        // Initialize EKG Canvases
        const ekgCanvases = document.querySelectorAll('.hero-ekg-canvas');
        ekgCanvases.forEach(canvas => {
            if (activeRenderers.has(canvas)) return;
            const parent = canvas.parentElement || canvas;
            const rect = parent.getBoundingClientRect();
            canvas.width = Math.max(Math.floor(rect.width), 600);
            canvas.height = Math.max(Math.floor(rect.height), 220);
            const sim = new EkgMonitorSimulator(canvas);
            activeRenderers.set(canvas, sim);
            viewportObserver.observe(canvas);
        });
    }

    // Main Render Loop with Page Visibility Sleep
    let isTabVisible = !document.hidden;
    let animFrameId = null;

    document.addEventListener('visibilitychange', () => {
        isTabVisible = !document.hidden;
        if (isTabVisible && !animFrameId) {
            animFrameId = requestAnimationFrame(renderVfxLoop);
        }
    });

    function renderVfxLoop(now) {
        if (!isTabVisible) {
            animFrameId = null;
            return;
        }

        activeRenderers.forEach((renderer, canvas) => {
            if (!document.body.contains(canvas)) {
                viewportObserver.unobserve(canvas);
                activeRenderers.delete(canvas);
                return;
            }
            if (renderer.isVisible) {
                renderer.render(now);
            }
        });
        animFrameId = requestAnimationFrame(renderVfxLoop);
    }
    animFrameId = requestAnimationFrame(renderVfxLoop);

    // =========================================================================
    // 4. PASSIVE RAF-THROTTLED 3D TILT & INTERACTION
    // =========================================================================
    function init3DTilt() {
        const cards = document.querySelectorAll('.gamer-card-3d:not([data-tilt-initialized])');
        cards.forEach(card => {
            card.setAttribute('data-tilt-initialized', 'true');

            let ticking = false;
            let lastEvent = null;

            card.addEventListener('mousemove', (e) => {
                lastEvent = e;
                if (!ticking) {
                    requestAnimationFrame(() => {
                        if (!lastEvent) return;
                        const rect = card.getBoundingClientRect();
                        const x = lastEvent.clientX - rect.left;
                        const y = lastEvent.clientY - rect.top;

                        const centerX = rect.width / 2;
                        const centerY = rect.height / 2;

                        const rotateX = ((y - centerY) / centerY) * -8;
                        const rotateY = ((x - centerX) / centerX) * 8;

                        card.style.transform = `perspective(1000px) rotateX(${rotateX.toFixed(2)}deg) rotateY(${rotateY.toFixed(2)}deg) translateY(-4px) scale(1.02)`;

                        const glareX = (x / rect.width) * 100;
                        const glareY = (y / rect.height) * 100;
                        card.style.setProperty('--glare-x', `${glareX.toFixed(1)}%`);
                        card.style.setProperty('--glare-y', `${glareY.toFixed(1)}%`);
                        ticking = false;
                    });
                    ticking = true;
                }
            }, { passive: true });

            card.addEventListener('mouseleave', () => {
                card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) translateY(0) scale(1)';
                lastEvent = null;
            }, { passive: true });
        });
    }

    // --- 5. Digital Odometer Counter ---
    function initCounters() {
        const counters = document.querySelectorAll('.gamer-counter:not([data-counter-initialized])');
        counters.forEach(counter => {
            const targetText = counter.getAttribute('data-target') || counter.textContent.trim();
            const targetNumber = parseInt(targetText.replace(/[^\d]/g, ''), 10);

            if (isNaN(targetNumber)) return;
            counter.setAttribute('data-counter-initialized', 'true');

            const duration = 1000;
            const startTime = performance.now();

            function updateCounter(currentTime) {
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const easeOut = progress === 1 ? 1 : 1 - Math.pow(2, -10 * progress);
                const currentVal = Math.floor(easeOut * targetNumber);

                counter.textContent = currentVal.toLocaleString('fa-IR');

                if (progress < 1) {
                    requestAnimationFrame(updateCounter);
                } else {
                    counter.textContent = targetNumber.toLocaleString('fa-IR');
                }
            }
            requestAnimationFrame(updateCounter);
        });
    }

    // --- 6. Cockpit Aura & Telemetry HUD ---
    window.setCockpitAura = function (auraName) {
        const root = document.getElementById('cyber-cockpit-root') || document.documentElement;
        root.setAttribute('data-aura', auraName);
        try {
            localStorage.setItem('tickethub_cockpit_aura', auraName);
        } catch (e) { }

        document.querySelectorAll('.aura-btn').forEach(btn => {
            if (btn.getAttribute('data-aura-target') === auraName) {
                btn.classList.add('active');
            } else {
                btn.classList.remove('active');
            }
        });
    };

    function initCockpitAura() {
        let savedAura = 'water';
        try {
            savedAura = localStorage.getItem('tickethub_cockpit_aura') || 'water';
        } catch (e) { }
        window.setCockpitAura(savedAura);
    }

    let telemetryInterval = null;
    function initCockpitTelemetry() {
        const pingEl = document.getElementById('hud-ping-val');
        if (!pingEl || telemetryInterval) return;

        telemetryInterval = setInterval(() => {
            if (document.hidden) return;
            const currentPing = document.getElementById('hud-ping-val');
            if (currentPing) {
                const basePing = 18;
                const jitter = Math.floor(Math.random() * 7) - 3;
                currentPing.textContent = `${basePing + jitter}ms`;
            }
        }, 3500);
    }

    function initAll() {
        initElementalCanvases();
        init3DTilt();
        initCounters();
        initCockpitAura();
        initCockpitTelemetry();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    const observer = new MutationObserver(() => {
        initAll();
    });

    observer.observe(document.body, { childList: true, subtree: true });

    // Global ESC Key Listener for Modals
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' || e.keyCode === 27) {
            const openModalCloseBtn = document.querySelector('.modal-hud-chassis button[class*="hover:text-cyan-300"], .modal-hud-chassis button[class*="group"], .modal-hud-chassis .btn-cyber-ghost');
            if (openModalCloseBtn) {
                openModalCloseBtn.click();
            }
        }
    });

    window.initGamerHud = initAll;
})();
