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

    // --- D. VIBRANT BOILING RADIOACTIVE ACID BUBBLES (LAVA LAMP) ---
    const FRAGMENT_TOXIC_SRC = GLSL_COMMON_FUNCTIONS + `
        void main() {
            vec2 uv = v_uv;
            float t = u_time * 0.8; // Boiling speed

            // Mask to hide bubbles before they hit the text
            float poolMask = 1.0 - smoothstep(0.3, 0.65, uv.y);

            // Interactive mouse liquid disturbance
            vec2 mouseOffset = vec2(0.0);
            if (u_mouse.x > 0.0) {
                vec2 mNorm = u_mouse / u_resolution;
                vec2 toMouse = uv - mNorm;
                float d = length(toMouse);
                if (d < 0.4) {
                    float force = (1.0 - d / 0.4) * 0.05;
                    mouseOffset = normalize(toMouse) * force * sin(d * 20.0 - t * 4.0);
                }
            }
            vec2 dUv = uv + mouseOffset;

            // 1. Base fluid level (creates a solid wavy pool at the bottom)
            float surfaceY = 0.06 + sin(dUv.x * 15.0 + t * 1.5) * 0.015 + cos(dUv.x * 8.0 - t) * 0.01;
            float denom = dUv.y - surfaceY;
            float field = 0.0;
            if(denom < 0.0) {
                field = 100.0; // Solid liquid below surface
            } else {
                field = 0.012 / max(0.0001, denom); 
            }

            // 2. Add rising, merging metaball bubbles
            const int NUM_BUBBLES = 14;
            for(int i = 0; i < NUM_BUBBLES; i++) {
                float id = float(i);
                float radius = 0.005 + fract(sin(id * 73.1) * 192.4) * 0.01; // Random sizes
                float speed = 0.1 + fract(sin(id * 31.3) * 21.2) * 0.15;
                float startX = fract(sin(id * 92.4) * 51.5); // Random X position
                
                // Organic wobble and rise
                float x = startX + sin(t * 1.2 + id * 5.0) * 0.05; 
                
                // Bubbles spawn inside the pool and rise up
                float y = fract(t * speed + id * 0.618) * 0.7 - 0.05; 
                
                vec2 p = vec2(x, y);
                
                // Minor distance distortion for organic shapes
                vec2 distP = dUv;
                distP.x += sin(dUv.y * 20.0 + t * 2.0) * 0.003;
                
                float d = length(distP - p);
                
                field += radius / (d * d + 0.0001); 
            }

            // 3. Define the fluid boundary (metaball threshold)
            float isLiquid = smoothstep(18.0, 22.0, field);
            
            // 4. Acid colors
            vec3 deepBlack = vec3(0.01, 0.03, 0.01);
            vec3 darkGreen = vec3(0.05, 0.35, 0.1);
            vec3 neonLime = vec3(0.3, 0.95, 0.15);
            vec3 toxicWhite = vec3(0.8, 1.0, 0.7);

            // Shading layers based on field density
            float edgeGlow = smoothstep(18.0, 30.0, field) - smoothstep(30.0, 60.0, field);
            float core = smoothstep(25.0, 90.0, field);
            float specular = smoothstep(90.0, 200.0, field);

            vec3 col = deepBlack;
            col = mix(col, darkGreen, edgeGlow);
            col = mix(col, neonLime, core);
            col += toxicWhite * specular * 0.9;

            // 5. Tiny ambient fizzy particles floating up
            float fizz = 0.0;
            for(int i = 0; i < 10; i++) {
                float fi = float(i);
                float vY = fract(t * 0.4 + fi * 0.33) * 0.8;
                float vX = fract(sin(fi * 11.1) * 33.3) + sin(t * 1.5 + fi) * 0.04;
                float vD = length(uv - vec2(vX, vY));
                fizz += smoothstep(0.01, 0.004, vD) * (1.0 - smoothstep(0.3, 0.6, vY));
            }
            col += neonLime * fizz * 0.8;

            // Combine masks
            float alpha = clamp((isLiquid + fizz) * poolMask, 0.0, 0.95);
            
            gl_FragColor = vec4(col * alpha, alpha);
        }
    `;

    // --- E. MULTI-COLUMN REALISTIC CURLING SMOKE WISPS SHADER (IMAGE 2 INSPIRATION) ---
    const FRAGMENT_SMOKE_SRC = GLSL_COMMON_FUNCTIONS + `
        // Single Sinuous Smoke Wisp Generator (Image 2 style)
        float smokeWisp(vec2 uv, float originX, float time, float widthScale, float swayFreq, float swayAmp) {
            float y = uv.y;
            // Laminar root at base transitioning to turbulent curling plume as it rises
            float curlTransition = smoothstep(0.02, 0.75, y);
            
            // Sinuous S-curve horizontal displacement
            float sway = sin(y * swayFreq * 6.28 - time * 2.0) * swayAmp * curlTransition;
            sway += sin(y * (swayFreq * 2.2) * 6.28 + time * 1.5) * (swayAmp * 0.4) * curlTransition;
            
            // Fibrous tendril noise distortion
            vec2 tendrilP = vec2(uv.x * 16.0, y * 4.5 - time * 1.2);
            float fibrousNoise = fbm(tendrilP) * 0.035 * curlTransition;

            float centerX = originX + sway + fibrousNoise;
            float distFromCenter = abs(uv.x - centerX);

            // Plume width expands organically as it rises
            float plumeWidth = (0.018 + y * 0.065) * widthScale;
            
            // Core wisp density with soft falloff
            float density = 1.0 - smoothstep(0.0, plumeWidth, distFromCenter);
            
            // Vertical dissipation
            float verticalFade = smoothstep(0.0, 0.06, y) * (1.0 - smoothstep(0.45, 0.85, y));
            
            // Internal fibrous strand turbulence
            float strandNoise = fbm(vec2(uv.x * 24.0, y * 7.0 - time * 1.8));
            float strands = smoothstep(0.15, 0.85, strandNoise);

            return pow(density, 1.6) * verticalFade * (0.65 + strands * 0.55);
        }

        void main() {
            vec2 uv = v_uv;
            float t = u_time * 0.75;

            // Interactive mouse wind deflection
            if (u_mouse.x > 0.0) {
                vec2 mNorm = u_mouse / u_resolution;
                vec2 toMouse = uv - mNorm;
                float d = length(toMouse);
                if (d < 0.45) {
                    float force = (1.0 - d / 0.45) * 0.16;
                    uv.x += (toMouse.x > 0.0 ? force : -force) * (1.0 - uv.y * 0.5);
                }
            }

            // Combine 4 distinct graceful rising plumes across the card (Image 2 style)
            float wisp1 = smokeWisp(uv, 0.22, t * 1.0,  1.0, 0.9, 0.045);
            float wisp2 = smokeWisp(uv, 0.45, t * 0.85, 1.2, 1.2, 0.055);
            float wisp3 = smokeWisp(uv, 0.68, t * 1.15, 0.9, 1.1, 0.040);
            float wisp4 = smokeWisp(uv, 0.85, t * 0.95, 1.1, 0.8, 0.050);

            // Ambient background mist drift
            vec2 ambientP = uv * vec2(2.5, 1.8) + vec2(t * 0.15, -t * 0.6);
            float ambientMist = fbm(ambientP) * (1.0 - smoothstep(0.02, 0.65, uv.y)) * 0.22;

            float totalSmoke = clamp(wisp1 + wisp2 * 1.1 + wisp3 * 0.9 + wisp4 + ambientMist, 0.0, 1.0);

            if (totalSmoke <= 0.01) {
                gl_FragColor = vec4(0.0);
                return;
            }

            // Pearlescent Soft Silver & Billowing White Smoke Palette
            vec3 deepMist   = vec3(0.45, 0.52, 0.62);
            vec3 softSilver = vec3(0.82, 0.88, 0.94);
            vec3 pureWhite  = vec3(1.0, 1.0, 1.0);
            vec3 silkyGlow  = vec3(0.95, 0.98, 1.0);

            vec3 col = mix(deepMist, softSilver, smoothstep(0.1, 0.5, totalSmoke));
            col = mix(col, pureWhite, smoothstep(0.45, 0.9, totalSmoke));
            col += silkyGlow * (pow(totalSmoke, 2.5) * 0.45);

            float alpha = clamp(totalSmoke * 0.92, 0.0, 0.94);
            gl_FragColor = vec4(col * alpha, alpha);
        }
    `;

    // --- F. COSMIC CHRONO VOID & CRIMSON GRAVITATIONAL SINGULARITY SHADER (LOCALIZED BOTTOM) ---
    const FRAGMENT_VOID_SRC = GLSL_COMMON_FUNCTIONS + `
        void main() {
            vec2 uv = v_uv;
            float t = u_time * 1.4;

            // Bottom mask: confine void singularity strictly to lower portion of the card
            float bottomMask = 1.0 - smoothstep(0.04, 0.58, uv.y);

            // Singularity center fixed at bottom-center
            vec2 center = vec2(0.5, 0.08);
            vec2 p = (uv - center) * vec2(u_resolution.x / u_resolution.y, 1.0);

            // Mouse gravitational distortion
            if (u_mouse.x > 0.0) {
                vec2 mNorm = (u_mouse / u_resolution - center) * vec2(u_resolution.x / u_resolution.y, 1.0);
                float mDist = length(p - mNorm);
                p += (mNorm - p) * (1.0 - smoothstep(0.0, 0.5, mDist)) * 0.2;
            }

            float r = length(p);
            float angle = atan(p.y, p.x);

            // Swirling gravitational vortex
            float spiral = angle + 4.0 / (r + 0.12) - t * 1.8;
            vec2 spiralUv = vec2(sin(spiral), cos(spiral)) * r * 2.2;

            // Cosmic plasma turbulence
            float plasma1 = fbm(spiralUv * 3.2 + vec2(t * 0.6, -t * 0.8));
            float plasma2 = fbm(vec2(r * 9.0 - t * 2.8, angle * 2.0 + t));
            float accretion = clamp(plasma1 * 0.6 + plasma2 * 0.5, 0.0, 1.0);

            // Accretion Disk Rings & Singularity
            float diskRing = 1.0 / (abs(r - 0.24) * 24.0 + 1.0);
            float outerRing = 1.0 / (abs(r - 0.44) * 18.0 + 1.0);
            float innerSingularity = 1.0 - smoothstep(0.0, 0.12, r);

            // Radial Pulsing Chrono Shockwave
            float shockwave = sin(r * 26.0 - t * 5.5);
            shockwave = smoothstep(0.7, 1.0, shockwave) * (1.0 - smoothstep(0.08, 0.55, r));

            // Deep Cosmic Void & Neon Crimson/Violet Palette
            vec3 deepVoid = vec3(0.02, 0.01, 0.06);
            vec3 neonViolet = vec3(0.55, 0.08, 0.85);
            vec3 bloodCrimson = vec3(0.95, 0.08, 0.25);
            vec3 pulsarWhite = vec3(1.0, 0.95, 1.0);
            vec3 chronoGold = vec3(1.0, 0.65, 0.15);

            vec3 col = deepVoid;
            col += neonViolet * (accretion * 1.1 + outerRing * 0.9);
            col += bloodCrimson * (diskRing * 2.0 + shockwave * 1.4);
            col += pulsarWhite * pow(diskRing, 2.5) * 1.8;
            col += chronoGold * (shockwave * 0.8);

            // Darken the actual black hole core
            col *= (1.0 - innerSingularity * 0.92);

            float alpha = clamp((accretion * 0.6 + diskRing * 0.8 + outerRing * 0.4 + shockwave * 0.5) * bottomMask * (1.0 - innerSingularity * 0.4), 0.0, 0.92);
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
            if (!gl.__programCache) {
                gl.__programCache = new Map();
                gl.__shaderCache = new Map();
            }

            let prog = gl.__programCache.get(fragmentSrc);
            if (!prog) {
                let vs = gl.__shaderCache.get(VERTEX_SHADER_SRC);
                if (!vs) {
                    vs = this.compileShader(VERTEX_SHADER_SRC, gl.VERTEX_SHADER);
                    if (vs) gl.__shaderCache.set(VERTEX_SHADER_SRC, vs);
                }

                let fs = gl.__shaderCache.get(fragmentSrc);
                if (!fs) {
                    fs = this.compileShader(fragmentSrc, gl.FRAGMENT_SHADER);
                    if (fs) gl.__shaderCache.set(fragmentSrc, fs);
                }

                if (!vs || !fs) return;

                prog = gl.createProgram();
                gl.attachShader(prog, vs);
                gl.attachShader(prog, fs);
                gl.linkProgram(prog);
                if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) {
                    console.error('Program Link Error:', gl.getProgramInfoLog(prog));
                    return;
                }
                gl.__programCache.set(fragmentSrc, prog);
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

        destroy() {
            try {
                this.isVisible = false;
                this.gl = null;
                this.program = null;
            } catch (e) { }
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
            const midY = h * 0.5;
            const midPoint = w * 0.55;

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
                    this.currentSpikeScale = 0.35 + Math.random() * 0.08;
                    this.currentPScale = 0.08 + Math.random() * 0.04;
                    this.currentTScale = 0.14 + Math.random() * 0.06;
                }

                if (this.heartbeatPhase > 0) {
                    this.heartbeatPhase++;
                    if (this.heartbeatPhase === 2) return midY - (h * this.currentPScale); // P wave
                    if (this.heartbeatPhase === 3) return midY + (h * (this.currentPScale * 0.5)); // Q dip
                    if (this.heartbeatPhase === 4) return midY - (h * this.currentSpikeScale); // R spike
                    if (this.heartbeatPhase === 5) return midY + (h * (this.currentSpikeScale * 0.5)); // S dip
                    if (this.heartbeatPhase === 6) return midY - (h * this.currentTScale); // T wave
                    if (this.heartbeatPhase === 7) return midY - (h * 0.04); // U wave
                    if (this.heartbeatPhase > 7) {
                        this.heartbeatPhase = 0;
                        return midY;
                    }
                }
                // Organic biological baseline wave
                return midY + Math.sin(this.scanX * 0.15) * 0.6 + (Math.random() - 0.5) * 0.8;
            }
        }

        updateDomStatus(state) {
            const badge = document.querySelector('.hero-ekg-status');
            if (!badge) return;
            const txt = state === 'normal' ? '78 BPM // STABLE' : '0 BPM // FLATLINE';
            if (badge.firstChild && badge.firstChild.nodeType === 3) {
                badge.firstChild.nodeValue = txt;
            } else {
                badge.textContent = txt;
            }
            if (state === 'normal') {
                badge.className = 'hero-ekg-status text-[9px] font-mono text-emerald-400 font-bold';
            } else {
                badge.className = 'hero-ekg-status text-[9px] font-mono text-rose-400 font-bold animate-pulse';
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

            const midPoint = w * 0.55;

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
                ctx.lineWidth = 3.0;
                ctx.globalAlpha = 0.4;
                this.drawSmoothPath(ctx, greenPoints);

                // White-Hot Core Laser Line
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.3;
                ctx.globalAlpha = 0.95;
                this.drawSmoothPath(ctx, greenPoints);
            }

            // 2. Draw Red Organic Flatline Path (Second Half)
            if (redPoints.length >= 2) {
                // Outer Crimson Glow
                ctx.strokeStyle = '#f43f5e';
                ctx.lineWidth = 3.0;
                ctx.globalAlpha = 0.4;
                this.drawSmoothPath(ctx, redPoints);

                // White-Hot Core Laser Line
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.3;
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

    const pendingCanvasesQueue = [];
    let isProcessingVfxQueue = false;

    function queueVfxCanvas(canvas) {
        if (!canvas) return;
        if (!pendingCanvasesQueue.includes(canvas)) {
            pendingCanvasesQueue.push(canvas);
        }
        if (!isProcessingVfxQueue) {
            isProcessingVfxQueue = true;
            requestAnimationFrame(processNextVfxCanvas);
        }
    }

    function processNextVfxCanvas() {
        if (pendingCanvasesQueue.length === 0) {
            isProcessingVfxQueue = false;
            return;
        }
        const canvas = pendingCanvasesQueue.shift();
        if (canvas && document.body.contains(canvas) && !activeRenderers.has(canvas)) {
            if (canvas.classList.contains('hero-ekg-canvas')) {
                const parent = canvas.parentElement || canvas;
                const rect = parent.getBoundingClientRect();
                const targetW = Math.max(Math.floor(rect.width), 160);
                const targetH = Math.max(Math.floor(rect.height), 32);
                if (canvas.width !== targetW) canvas.width = targetW;
                if (canvas.height !== targetH) canvas.height = targetH;
                const sim = new EkgMonitorSimulator(canvas);
                activeRenderers.set(canvas, sim);
                viewportObserver.observe(canvas);
                ensureVfxLoopRunning();
            } else {
                const element = canvas.getAttribute('data-element') || 'none';
                const rect = canvas.getBoundingClientRect();
                const targetW = Math.max(Math.floor(rect.width), 200);
                const targetH = Math.max(Math.floor(rect.height), 120);
                if (canvas.width !== targetW) canvas.width = targetW;
                if (canvas.height !== targetH) canvas.height = targetH;

                let shaderSrc = null;
                if (element === 'fire') shaderSrc = FRAGMENT_FIRE_SRC;
                else if (element === 'water') shaderSrc = FRAGMENT_WATER_SRC;
                else if (element === 'lightning') shaderSrc = FRAGMENT_LIGHTNING_SRC;
                else if (element === 'toxic') shaderSrc = FRAGMENT_TOXIC_SRC;
                else if (element === 'smoke') shaderSrc = FRAGMENT_SMOKE_SRC;
                else if (element === 'void') shaderSrc = FRAGMENT_VOID_SRC;

                if (shaderSrc) {
                    const renderer = new WebGLShaderRenderer(canvas, shaderSrc);
                    activeRenderers.set(canvas, renderer);
                    viewportObserver.observe(canvas);
                    ensureVfxLoopRunning();
                }
            }
        }

        if (pendingCanvasesQueue.length > 0) {
            requestAnimationFrame(processNextVfxCanvas);
        } else {
            isProcessingVfxQueue = false;
        }
    }

    window.ensureDashboardVfxReady = function () {
        const canvases = document.querySelectorAll('.element-vfx-canvas');
        canvases.forEach(canvas => {
            if (!activeRenderers.has(canvas)) {
                queueVfxCanvas(canvas);
            }
        });

        // Initialize EKG Canvases
        const ekgCanvases = document.querySelectorAll('.hero-ekg-canvas');
        ekgCanvases.forEach(canvas => {
            if (!activeRenderers.has(canvas)) {
                queueVfxCanvas(canvas);
            }
        });

        return true;
    };

    // Auto-observe DOM for dynamically mounted canvases (e.g. after skeleton finishes)
    if (typeof MutationObserver !== 'undefined') {
        const domObserver = new MutationObserver((mutations) => {
            let foundCanvas = false;
            for (const mutation of mutations) {
                if (mutation.addedNodes.length > 0) {
                    for (const node of mutation.addedNodes) {
                        if (node.nodeType === 1) { // ELEMENT_NODE
                            if (node.matches && (node.matches('.element-vfx-canvas') || node.matches('.hero-ekg-canvas'))) {
                                queueVfxCanvas(node);
                                foundCanvas = true;
                            } else if (node.querySelectorAll) {
                                const matched = node.querySelectorAll('.element-vfx-canvas, .hero-ekg-canvas');
                                matched.forEach(c => {
                                    queueVfxCanvas(c);
                                    foundCanvas = true;
                                });
                            }
                        }
                    }
                }
            }
        });
        domObserver.observe(document.body, { childList: true, subtree: true });
    }

    function initElementalCanvases() {
        return window.ensureDashboardVfxReady();
    }

    // Main Render Loop with 60 FPS Capping & Page Visibility Sleep
    let isTabVisible = !document.hidden;
    let animFrameId = null;
    let lastVfxFrameTime = 0;
    const VFX_FRAME_INTERVAL = 1000 / 60; // 60 FPS cap (16.6ms)

    function ensureVfxLoopRunning() {
        if (isTabVisible && !animFrameId && activeRenderers.size > 0) {
            animFrameId = requestAnimationFrame(renderVfxLoop);
        }
    }

    document.addEventListener('visibilitychange', () => {
        isTabVisible = !document.hidden;
        if (isTabVisible) {
            ensureVfxLoopRunning();
        } else {
            animFrameId = null;
        }
    });

    function renderVfxLoop(now) {
        if (!isTabVisible || activeRenderers.size === 0) {
            animFrameId = null;
            return;
        }

        animFrameId = requestAnimationFrame(renderVfxLoop);

        const delta = now - lastVfxFrameTime;
        if (delta < VFX_FRAME_INTERVAL) {
            return;
        }
        lastVfxFrameTime = now - (delta % VFX_FRAME_INTERVAL);

        activeRenderers.forEach((renderer, canvas) => {
            if (!document.body.contains(canvas)) {
                viewportObserver.unobserve(canvas);
                if (renderer && typeof renderer.destroy === 'function') {
                    renderer.destroy();
                }
                activeRenderers.delete(canvas);
                return;
            }
            if (renderer.isVisible) {
                renderer.render(now);
            }
        });
    }
    ensureVfxLoopRunning();

    // =========================================================================
    // =========================================================================
    // 4. PASSIVE RAF-THROTTLED 3D TILT & INTERACTION
    // =========================================================================
    function init3DTilt() {
        const cards = document.querySelectorAll('.gamer-card-3d:not([data-tilt-initialized])');
        cards.forEach(card => {
            card.dataset.tiltInitialized = 'true';

            let ticking = false;
            let lastEvent = null;

            card.addEventListener('mousemove', (e) => {
                lastEvent = e;
                if (!ticking) {
                    requestAnimationFrame(() => {
                        if (!lastEvent || !card.isConnected) return;
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
                if (card.isConnected) {
                    card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) translateY(0) scale(1)';
                }
                lastEvent = null;
            }, { passive: true });
        });
    }

    // --- 5. Digital Odometer Counter ---
    function initCounters() {
        const counters = document.querySelectorAll('.gamer-counter');
        counters.forEach(counter => {
            if (!counter.isConnected) return;
            const targetText = counter.getAttribute('data-target') || (counter.textContent || '').trim();
            const targetNumber = parseInt(targetText.replace(/[^\d]/g, ''), 10);

            if (isNaN(targetNumber)) return;

            const currentTarget = counter.getAttribute('data-current-target');
            if (currentTarget === String(targetNumber)) {
                return;
            }

            counter.setAttribute('data-current-target', String(targetNumber));

            const prevVal = parseInt((counter.textContent || '0').replace(/[^\d]/g, ''), 10);
            const startVal = isNaN(prevVal) ? 0 : prevVal;

            function setCounterText(val) {
                if (!counter.isConnected) return;
                const faStr = val.toLocaleString('fa-IR');
                counter.textContent = faStr;
            }

            if (startVal === targetNumber) {
                setCounterText(targetNumber);
                return;
            }

            const duration = 600;
            const startTime = performance.now();

            function updateCounter(currentTime) {
                if (!counter.isConnected) return;
                const elapsed = currentTime - startTime;
                const progress = Math.min(elapsed / duration, 1);
                const easeOut = progress === 1 ? 1 : 1 - Math.pow(2, -10 * progress);
                const currentVal = Math.floor(startVal + easeOut * (targetNumber - startVal));

                setCounterText(currentVal);

                if (progress < 1) {
                    requestAnimationFrame(updateCounter);
                } else {
                    setCounterText(targetNumber);
                }
            }
            requestAnimationFrame(updateCounter);
        });
    }

    // --- 6. Cyber Audio Synthesizer Engine (Web Audio API) ---
    let audioCtx = null;
    let masterGain = null;
    let compressor = null;
    let lastSoundTime = 0;
    let lastSoundType = '';

    function getAudioContext() {
        if (!audioCtx) {
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (AudioContextClass) {
                audioCtx = new AudioContextClass();
                
                // Dynamics compressor to ensure silky smooth output without distortion or clipping
                compressor = audioCtx.createDynamicsCompressor();
                compressor.threshold.setValueAtTime(-20, audioCtx.currentTime);
                compressor.knee.setValueAtTime(15, audioCtx.currentTime);
                compressor.ratio.setValueAtTime(10, audioCtx.currentTime);
                compressor.attack.setValueAtTime(0.002, audioCtx.currentTime);
                compressor.release.setValueAtTime(0.12, audioCtx.currentTime);

                masterGain = audioCtx.createGain();
                masterGain.gain.setValueAtTime(0.75, audioCtx.currentTime);

                masterGain.connect(compressor);
                compressor.connect(audioCtx.destination);
            }
        }
        if (audioCtx && audioCtx.state === 'suspended') {
            audioCtx.resume();
        }
        return audioCtx;
    }

    window.playCyberSound = function (type) {
        try {
            const soundEnabled = localStorage.getItem('tickethub_sound_fx') !== 'false';
            if (!soundEnabled) return;

            const nowPerf = performance.now();
            // Jitter & debounce protection: prevent rapid stutter/stacking
            if (nowPerf - lastSoundTime < 45 && lastSoundType === type) {
                return;
            }
            lastSoundTime = nowPerf;
            lastSoundType = type;

            const ctx = getAudioContext();
            if (!ctx || !masterGain) return;

            const now = ctx.currentTime;

            if (type === 'menu' || type === 'nav') {
                // 1. Menu & Submenu Navigation: Soft, sleek holographic glass blip
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(540, now);
                osc.frequency.exponentialRampToValueAtTime(780, now + 0.035);

                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.045, now + 0.004);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.035);

                osc.connect(gain);
                gain.connect(masterGain);
                osc.start(now);
                osc.stop(now + 0.04);
            }
            else if (type === 'create') {
                // 2. Module Creation Buttons: High-tech futuristic engage/uplink dual chime
                // Note 1: Fast rising sine pulse
                const osc1 = ctx.createOscillator();
                const gain1 = ctx.createGain();
                osc1.type = 'sine';
                osc1.frequency.setValueAtTime(440, now);
                osc1.frequency.exponentialRampToValueAtTime(880, now + 0.045);

                gain1.gain.setValueAtTime(0.0001, now);
                gain1.gain.exponentialRampToValueAtTime(0.055, now + 0.005);
                gain1.gain.exponentialRampToValueAtTime(0.0001, now + 0.06);

                osc1.connect(gain1);
                gain1.connect(masterGain);
                osc1.start(now);
                osc1.stop(now + 0.065);

                // Note 2: Harmonic high spark
                const osc2 = ctx.createOscillator();
                const gain2 = ctx.createGain();
                osc2.type = 'triangle';
                osc2.frequency.setValueAtTime(880, now + 0.025);
                osc2.frequency.exponentialRampToValueAtTime(1320, now + 0.08);

                gain2.gain.setValueAtTime(0.0001, now + 0.025);
                gain2.gain.exponentialRampToValueAtTime(0.045, now + 0.03);
                gain2.gain.exponentialRampToValueAtTime(0.0001, now + 0.085);

                osc2.connect(gain2);
                gain2.connect(masterGain);
                osc2.start(now + 0.025);
                osc2.stop(now + 0.09);
            }
            else if (type === 'submit' || type === 'save' || type === 'success') {
                // 3. Modal Submit / Save Buttons: Satisfying 3-tone cyber confirmation arpeggio (C5 -> E5 -> C6)
                const notes = [
                    { freq: 523.25, time: 0, dur: 0.12, vol: 0.045 },
                    { freq: 659.25, time: 0.035, dur: 0.14, vol: 0.045 },
                    { freq: 1046.50, time: 0.07, dur: 0.18, vol: 0.055 }
                ];

                notes.forEach(n => {
                    const o = ctx.createOscillator();
                    const g = ctx.createGain();
                    o.type = 'sine';
                    o.frequency.setValueAtTime(n.freq, now + n.time);

                    g.gain.setValueAtTime(0.0001, now + n.time);
                    g.gain.exponentialRampToValueAtTime(n.vol, now + n.time + 0.006);
                    g.gain.exponentialRampToValueAtTime(0.0001, now + n.time + n.dur);

                    o.connect(g);
                    g.connect(masterGain);
                    o.start(now + n.time);
                    o.stop(now + n.time + n.dur + 0.01);
                });
            }
            else if (type === 'close' || type === 'cancel') {
                // 4. Modal Close / Cancel Buttons: Gentle soft cyber power-down tone
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(580, now);
                osc.frequency.exponentialRampToValueAtTime(320, now + 0.055);

                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.04, now + 0.004);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.055);

                osc.connect(gain);
                gain.connect(masterGain);
                osc.start(now);
                osc.stop(now + 0.06);
            }
            else if (type === 'aura') {
                // Futuristic Power-Surge Sound for theme switches
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(220, now);
                osc.frequency.exponentialRampToValueAtTime(660, now + 0.12);
                osc.frequency.exponentialRampToValueAtTime(440, now + 0.22);

                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.06, now + 0.02);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.22);

                osc.connect(gain);
                gain.connect(masterGain);
                osc.start(now);
                osc.stop(now + 0.23);
            }
            else if (type === 'toggle') {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(440, now);
                osc.frequency.exponentialRampToValueAtTime(660, now + 0.04);

                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.035, now + 0.004);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.04);

                osc.connect(gain);
                gain.connect(masterGain);
                osc.start(now);
                osc.stop(now + 0.045);
            }
            else if (type === 'click' || type === 'tap') {
                // Fallback for click/tap invocations
                window.playCyberSound('menu');
            }
            else if (type === 'powerup' || type === 'open') {
                window.playCyberSound('create');
            }
            else if (type === 'error') {
                const osc = ctx.createOscillator();
                const gain = ctx.createGain();
                osc.type = 'sine';
                osc.frequency.setValueAtTime(240, now);
                osc.frequency.exponentialRampToValueAtTime(160, now + 0.08);

                gain.gain.setValueAtTime(0.0001, now);
                gain.gain.exponentialRampToValueAtTime(0.04, now + 0.005);
                gain.gain.exponentialRampToValueAtTime(0.0001, now + 0.08);

                osc.connect(gain);
                gain.connect(masterGain);
                osc.start(now);
                osc.stop(now + 0.09);
            }
        } catch (e) { }
    };

    // --- 6.1 Unified Cyber UI Click Sound Dispatcher ---
    let soundListenerInitialized = false;
    function initCyberSoundDispatcher() {
        if (soundListenerInitialized) return;
        soundListenerInitialized = true;

        document.addEventListener('click', (e) => {
            try {
                const target = e.target;
                if (!target) return;

                // Modal backdrop direct click check
                if (target.classList && (target.classList.contains('modal-backdrop-quantum') || target.classList.contains('modal-backdrop-animate'))) {
                    window.playCyberSound('close');
                    return;
                }

                // Find nearest interactive element
                const btn = target.closest('a, button, [role="button"], input[type="submit"], input[type="button"], .aura-btn, .esc-key-badge');
                if (!btn) return;

                // Explicit sound attribute override
                const explicitSound = btn.getAttribute('data-sound');
                if (explicitSound) {
                    if (explicitSound !== 'none') {
                        window.playCyberSound(explicitSound);
                    }
                    return;
                }

                const text = (btn.innerText || btn.textContent || '').trim();
                const isInsideModal = !!btn.closest('.modal-hud-chassis, .modal-backdrop-quantum, .modal-backdrop-animate, [class*="modal"]');

                // 1. Modal Close & Cancel Buttons
                if (
                    (isInsideModal && (
                        btn.matches('.btn-cyber-ghost, .btn-holo-flicker, button[class*="hover:text-cyan-300"], button[class*="hover:text-rose"]') ||
                        text.includes('انصراف') ||
                        text.includes('بستن') ||
                        text.includes('لغو') ||
                        btn.querySelector('svg path[d*="M6 18L18 6"]') // ✕ close icon
                    )) ||
                    (!isInsideModal && btn.matches('.btn-cyber-ghost') && (text.includes('انصراف') || text.includes('بستن') || text.includes('لغو')))
                ) {
                    window.playCyberSound('close');
                    return;
                }

                // 2. Modal Submit / Save Buttons
                if (
                    (isInsideModal && (
                        btn.matches('button[type="submit"], .btn-laser-charge, .btn-cyber-primary') ||
                        text.includes('ذخیره') ||
                        text.includes('ثبت') ||
                        text.includes('حذف کن') ||
                        text.includes('تایید') ||
                        text.includes('انتقال') ||
                        text.includes('ارسال')
                    )) ||
                    (!isInsideModal && btn.matches('form button[type="submit"]'))
                ) {
                    window.playCyberSound('submit');
                    return;
                }

                // 3. Module Creation Buttons ("دکمه‌های ایجاد ماژول‌ها")
                if (
                    !isInsideModal && (
                        btn.matches('.btn-cyber-primary') ||
                        text.includes('تیکت جدید') ||
                        text.includes('پروژه جدید') ||
                        text.includes('کاربر جدید') ||
                        text.includes('جریان کاری جدید') ||
                        text.includes('تعریف') ||
                        text.includes('ایجاد') ||
                        text.includes('ثبت کوئست')
                    )
                ) {
                    window.playCyberSound('create');
                    return;
                }

                // 4. Menus and Submenus ("منوها و زیر منوها")
                if (
                    btn.matches('.cyber-nav-link, .cyber-sub-link, nav a, nav button, .aura-btn') ||
                    btn.closest('nav') ||
                    (btn.closest('aside') && btn.matches('a, button')) ||
                    btn.matches('[data-nav], [role="tab"]')
                ) {
                    window.playCyberSound('menu');
                    return;
                }
            } catch (err) { }
        }, { capture: true, passive: true });
    }

    // --- 7. Cockpit Aura & Telemetry HUD ---
    window.setCockpitAura = function (auraName, playSound = false) {
        const root = document.getElementById('cyber-cockpit-root') || document.documentElement;
        const currentAura = root.getAttribute('data-aura');
        if (currentAura === auraName && document.body.getAttribute('data-aura') === auraName) {
            return;
        }

        root.setAttribute('data-aura', auraName);
        document.documentElement.setAttribute('data-aura', auraName);
        document.body.setAttribute('data-aura', auraName);

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

        // Trigger reactive audio feedback only on explicit user click
        if (playSound) {
            window.playCyberSound('aura');
        }
    };

    window.toggleMatrixRain = function (enabled) {
        try {
            localStorage.setItem('tickethub_matrix_rain', enabled ? 'true' : 'false');
        } catch (e) { }

        const canvas = document.getElementById('global-matrix-rain-canvas');
        if (canvas) {
            if (enabled) {
                canvas.style.display = 'block';
                if (window.matrixRain) {
                    window.matrixRain.init('global-matrix-rain-canvas');
                }
            } else {
                canvas.style.display = 'none';
                if (window.matrixRain) {
                    window.matrixRain.destroy();
                }
            }
        }
        window.playCyberSound('toggle');
    };

    window.toggleAmbientNebula = function (enabled) {
        try {
            localStorage.setItem('tickethub_ambient_nebula', enabled ? 'true' : 'false');
        } catch (e) { }

        const container = document.getElementById('cyber-ambient-nebula-container');
        if (container) {
            container.style.opacity = enabled ? '1' : '0';
            container.style.transition = 'opacity 0.5s ease-in-out';
        }
        window.playCyberSound('toggle');
    };

    window.toggleSoundFx = function (enabled) {
        try {
            localStorage.setItem('tickethub_sound_fx', enabled ? 'true' : 'false');
        } catch (e) { }
        if (enabled) {
            window.playCyberSound('submit');
        }
    };

    let cockpitAuraInitialized = false;
    function initCockpitAura() {
        if (cockpitAuraInitialized) return;
        cockpitAuraInitialized = true;

        let savedAura = 'water';
        try {
            savedAura = localStorage.getItem('tickethub_cockpit_aura') || 'water';
        } catch (e) { }
        window.setCockpitAura(savedAura, false);

        // Check Matrix Rain initial state
        try {
            const matrixRain = localStorage.getItem('tickethub_matrix_rain') === 'true';
            if (matrixRain) {
                const canvas = document.getElementById('global-matrix-rain-canvas');
                if (canvas && window.matrixRain) {
                    canvas.style.display = 'block';
                    window.matrixRain.init('global-matrix-rain-canvas');
                }
            }
        } catch (e) { }

        // Check Nebula initial state
        try {
            const nebula = localStorage.getItem('tickethub_ambient_nebula') !== 'false';
            const container = document.getElementById('cyber-ambient-nebula-container');
            if (container) {
                container.style.opacity = nebula ? '1' : '0';
            }
        } catch (e) { }
    }

    let telemetryInterval = null;
    function initCockpitTelemetry() {
        const pingEl = document.getElementById('hud-ping-val');
        if (!pingEl || telemetryInterval) return;

        function updatePing() {
            if (document.hidden) return;
            const currentPing = document.getElementById('hud-ping-val');
            if (currentPing) {
                const basePing = 18;
                const jitter = Math.floor(Math.random() * 7) - 3;
                currentPing.textContent = `${basePing + jitter}ms`;
            }
        }

        updatePing();
        telemetryInterval = setInterval(updatePing, 3500);
    }

    // --- 8. Ambient Nebula Parallax Inertia with Idle Sleep ---
    let nebulaParallaxInitialized = false;
    let targetParallaxX = 0;
    let targetParallaxY = 0;
    let currentParallaxX = 0;
    let currentParallaxY = 0;
    let parallaxRafId = null;

    function initAmbientNebulaParallax() {
        const container = document.getElementById('cyber-ambient-nebula-container');
        if (!container || nebulaParallaxInitialized) return;
        nebulaParallaxInitialized = true;

        window.addEventListener('mousemove', (e) => {
            if (document.hidden) return;
            const x = (e.clientX / window.innerWidth) - 0.5;
            const y = (e.clientY / window.innerHeight) - 0.5;
            targetParallaxX = x * 24; // Subtle 24px displacement
            targetParallaxY = y * 18;
            if (!parallaxRafId) {
                parallaxRafId = requestAnimationFrame(animateParallax);
            }
        }, { passive: true });

        function animateParallax() {
            if (!document.hidden && container) {
                const diffX = targetParallaxX - currentParallaxX;
                const diffY = targetParallaxY - currentParallaxY;

                if (Math.abs(diffX) > 0.02 || Math.abs(diffY) > 0.02) {
                    currentParallaxX += diffX * 0.04;
                    currentParallaxY += diffY * 0.04;
                    container.style.transform = `translate3d(${currentParallaxX.toFixed(2)}px, ${currentParallaxY.toFixed(2)}px, 0)`;
                    parallaxRafId = requestAnimationFrame(animateParallax);
                } else {
                    currentParallaxX = targetParallaxX;
                    currentParallaxY = targetParallaxY;
                    container.style.transform = `translate3d(${currentParallaxX.toFixed(2)}px, ${currentParallaxY.toFixed(2)}px, 0)`;
                    parallaxRafId = null; // Sleep when reached equilibrium!
                }
            } else {
                parallaxRafId = null;
            }
        }

        parallaxRafId = requestAnimationFrame(animateParallax);
    }

    // =========================================================================
    // 10. STEALTH / ECO PERFORMANCE MODE ENGINE
    // =========================================================================
    let isStealthMode = false;

    window.toggleStealthMode = function (forceVal) {
        if (typeof forceVal === 'boolean') {
            isStealthMode = forceVal;
        } else {
            isStealthMode = !isStealthMode;
        }

        try {
            localStorage.setItem('tickethub_stealth_mode', isStealthMode ? 'true' : 'false');
        } catch (e) { }

        applyStealthModeUI();
        window.playCyberSound(isStealthMode ? 'toggle' : 'powerup');
    };

    function applyStealthModeUI() {
        if (isStealthMode) {
            document.documentElement.classList.add('stealth-mode');
            document.body.classList.add('stealth-mode');
            document.querySelectorAll('.element-vfx-canvas').forEach(c => c.style.display = 'none');
        } else {
            document.documentElement.classList.remove('stealth-mode');
            document.body.classList.remove('stealth-mode');
            document.querySelectorAll('.element-vfx-canvas').forEach(c => c.style.display = '');
        }

        const modeBtns = document.querySelectorAll('#hud-eco-mode-btn, .hud-eco-toggle-btn');
        modeBtns.forEach(btn => {
            if (isStealthMode) {
                btn.classList.add('active-eco');
                btn.setAttribute('title', 'حالت کم‌مصرف فعال است (کلید E)');
            } else {
                btn.classList.remove('active-eco');
                btn.setAttribute('title', 'حالت گرافیکی کامل فعال است (کلید E)');
            }
        });
    }

    let stealthModeInitialized = false;
    function initStealthMode() {
        if (!stealthModeInitialized) {
            stealthModeInitialized = true;
            try {
                isStealthMode = localStorage.getItem('tickethub_stealth_mode') === 'true';
            } catch (e) { }
        }
        applyStealthModeUI();
    }

    // =========================================================================
    // 11. POWER-USER GLOBAL KEYBOARD SHORTCUTS ENGINE (Bilingual English/Persian)
    // =========================================================================
    window.toggleShortcutsModal = function () {
        const modal = document.getElementById('cyber-shortcuts-modal');
        if (modal) {
            if (modal.classList.contains('hidden')) {
                modal.classList.remove('hidden');
                modal.classList.add('flex');
                window.playCyberSound('open');
            } else {
                modal.classList.add('hidden');
                modal.classList.remove('flex');
                window.playCyberSound('close');
            }
        }
    };

    let keyboardShortcutsInitialized = false;
    function initKeyboardShortcuts() {
        if (keyboardShortcutsInitialized) return;
        keyboardShortcutsInitialized = true;

        document.addEventListener('keydown', (e) => {
            const tag = (e.target && e.target.tagName) ? e.target.tagName.toUpperCase() : '';
            const isEditing = tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || (e.target && e.target.isContentEditable);

            // Escape key: closes shortcuts modal or open modals
            if (e.key === 'Escape' || e.keyCode === 27) {
                const shortcutsModal = document.getElementById('cyber-shortcuts-modal');
                if (shortcutsModal && !shortcutsModal.classList.contains('hidden')) {
                    shortcutsModal.classList.add('hidden');
                    shortcutsModal.classList.remove('flex');
                    window.playCyberSound('close');
                    return;
                }
                const openModalCloseBtn = document.querySelector('.modal-hud-chassis button[class*="hover:text-cyan-300"], .modal-hud-chassis button[class*="group"], .modal-hud-chassis .btn-cyber-ghost');
                if (openModalCloseBtn) {
                    openModalCloseBtn.click();
                }
                return;
            }

            if (isEditing) return;

            // Shortcut: '?' or Shift+'/' or '؟' -> Shortcuts Modal
            if (e.key === '?' || e.key === '؟' || ((e.key === '/' || e.code === 'Slash') && e.shiftKey)) {
                e.preventDefault();
                window.toggleShortcutsModal();
                return;
            }

            // Shortcut: '/' or Persian '÷' (Quick Search Focus)
            if ((e.code === 'Slash' && !e.shiftKey) || e.key === '/' || e.key === '÷') {
                const searchInput = document.querySelector('input[data-search-input="true"], input[placeholder*="جستجو"], input[type="search"], .search-input, input[id*="search"]');
                if (searchInput) {
                    e.preventDefault();
                    searchInput.focus();
                    if (typeof searchInput.select === 'function') searchInput.select();
                    window.playCyberSound('tap');
                    return;
                }
            }

            // Shortcut: 'C' or 'N' or Persian 'ز' / 'د' -> New Ticket Modal
            if (e.code === 'KeyC' || e.code === 'KeyN' || e.key === 'c' || e.key === 'C' || e.key === 'n' || e.key === 'N' || e.key === 'ز' || e.key === 'د') {
                const newTicketBtn = document.querySelector('[data-shortcut-new-ticket], button.btn-cyber-primary');
                if (newTicketBtn) {
                    e.preventDefault();
                    newTicketBtn.click();
                    return;
                }
            }

            // Shortcut: 'E' or Persian 'ث' -> Toggle Stealth Eco Mode
            if (e.code === 'KeyE' || e.key === 'e' || e.key === 'E' || e.key === 'ث') {
                e.preventDefault();
                window.toggleStealthMode();
                return;
            }

            // Navigation Helper matching both relative and absolute NavLink hrefs
            function navTo(target) {
                const clean = target.replace(/^\//, '');
                const selector = `a[href="${target}"], a[href="/${clean}"], a[href="${clean}"], a[href="${clean}/"]`;
                const link = document.querySelector(selector);
                if (link) {
                    link.click();
                } else {
                    window.location.assign('/' + clean);
                }
            }

            // Shortcut: '1' to '4' or Persian digits -> Quick Navigation
            if (e.code === 'Digit1' || e.code === 'Numpad1' || e.key === '1' || e.key === '۱') {
                e.preventDefault();
                navTo('/');
            } else if (e.code === 'Digit2' || e.code === 'Numpad2' || e.key === '2' || e.key === '۲') {
                e.preventDefault();
                navTo('/tickets');
            } else if (e.code === 'Digit3' || e.code === 'Numpad3' || e.key === '3' || e.key === '۳') {
                e.preventDefault();
                navTo('/projects');
            } else if (e.code === 'Digit4' || e.code === 'Numpad4' || e.key === '4' || e.key === '۴') {
                e.preventDefault();
                navTo('/users');
            }
        });
    }

    // =========================================================================
    // 12. REAL-TIME NETWORK CONNECTIVITY HUD MONITOR
    // =========================================================================
    function showNetworkToast(isOnline) {
        let toast = document.getElementById('cyber-network-hud-toast');
        if (!toast) {
            toast = document.createElement('div');
            toast.id = 'cyber-network-hud-toast';
            document.body.appendChild(toast);
        }

        if (!isOnline) {
            toast.style.display = 'flex';
            toast.className = 'fixed top-4 left-1/2 -translate-x-1/2 z-[100000] px-4 py-2.5 rounded-2xl font-bold text-xs flex items-center gap-2.5 shadow-[0_0_35px_rgba(244,63,94,0.5)] backdrop-blur-2xl transition-all duration-300 bg-[#0c050a]/95 border border-rose-500/70 text-rose-200 select-none animate-bounce';
            toast.innerHTML = `
                <span class="relative flex h-2.5 w-2.5">
                    <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
                    <span class="relative inline-flex rounded-full h-2.5 w-2.5 bg-rose-500"></span>
                </span>
                <span>⚠️ ارتباط شبکه قطع شد // در حال تلاش برای اتصال مجدد...</span>
            `;
            window.playCyberSound('error');
        } else {
            toast.style.display = 'flex';
            toast.className = 'fixed top-4 left-1/2 -translate-x-1/2 z-[100000] px-4 py-2.5 rounded-2xl font-bold text-xs flex items-center gap-2.5 shadow-[0_0_35px_rgba(16,185,129,0.4)] backdrop-blur-2xl transition-all duration-300 bg-[#050f0c]/95 border border-emerald-500/70 text-emerald-200 select-none';
            toast.innerHTML = `
                <span class="relative flex h-2.5 w-2.5">
                    <span class="relative inline-flex rounded-full h-2.5 w-2.5 bg-emerald-400"></span>
                </span>
                <span>⚡ ارتباط سایبری برقرار شد // سیستم آنلاین است</span>
            `;
            window.playCyberSound('success');
            setTimeout(() => {
                if (toast && toast.parentElement) {
                    toast.style.display = 'none';
                }
            }, 3000);
        }
    }

    let networkMonitorInitialized = false;
    function initNetworkMonitor() {
        if (networkMonitorInitialized) return;
        networkMonitorInitialized = true;
        window.addEventListener('offline', () => showNetworkToast(false));
        window.addEventListener('online', () => showNetworkToast(true));
    }

    function initAll() {
        initElementalCanvases();
        init3DTilt();
        initCounters();
        initCockpitAura();
        initCockpitTelemetry();
        initAmbientNebulaParallax();
        initCyberSoundDispatcher();
        initStealthMode();
        initKeyboardShortcuts();
        initNetworkMonitor();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    let initAllTimer = null;
    function debouncedInitAll() {
        if (initAllTimer) return;
        initAllTimer = setTimeout(() => {
            initAllTimer = null;
            initElementalCanvases();
            init3DTilt();
            initCounters();
            applyStealthModeUI();
        }, 100);
    }

    window.initGamerHud = initAll;
    window.refreshGamerHud = debouncedInitAll;
})();
