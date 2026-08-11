/**
 * TicketHub Cinema-Grade Gamer HUD & Elemental VFX Engine
 * High-performance, GPU-accelerated HTML5 Canvas 2D particle simulation
 * 60 FPS procedural Fire, Lightning, Toxic Acid, and Water Wave emitters
 */

(function () {
    // =========================================================================
    // 1. ELEMENTAL CANVAS VFX ENGINE
    // =========================================================================

    class Particle {
        constructor() { this.reset(); }
        reset() {
            this.x = 0; this.y = 0;
            this.vx = 0; this.vy = 0;
            this.size = 0;
            this.maxLife = 1; this.life = 0;
            this.color = '';
            this.alpha = 1;
        }
    }

    // --- A. REAL FIRE & FLAME EMITTER ---
    class FireSimulator {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d', { alpha: true });
            this.particles = [];
            this.embers = [];
            this.maxParticles = 55;
            this.maxEmbers = 18;
            this.init();
        }

        init() {
            for (let i = 0; i < this.maxParticles; i++) this.particles.push(this.createFlame(true));
            for (let i = 0; i < this.maxEmbers; i++) this.embers.push(this.createEmber(true));
        }

        createFlame(initial = false) {
            const w = this.canvas.width;
            const h = this.canvas.height;
            return {
                x: w * 0.15 + Math.random() * (w * 0.7),
                y: initial ? h - Math.random() * (h * 0.4) : h + 5,
                vx: (Math.random() - 0.5) * 1.5,
                vy: -2.5 - Math.random() * 3.5,
                size: 14 + Math.random() * 22,
                life: initial ? Math.random() * 50 : 0,
                maxLife: 35 + Math.random() * 30,
                wobbleSpeed: 0.05 + Math.random() * 0.08,
                seed: Math.random() * 100
            };
        }

        createEmber(initial = false) {
            const w = this.canvas.width;
            const h = this.canvas.height;
            return {
                x: Math.random() * w,
                y: initial ? Math.random() * h : h + 5,
                vx: (Math.random() - 0.5) * 2.2,
                vy: -3.0 - Math.random() * 4.5,
                size: 1.5 + Math.random() * 3.0,
                life: initial ? Math.random() * 60 : 0,
                maxLife: 50 + Math.random() * 40,
                color: Math.random() > 0.3 ? '#fef08a' : '#f97316'
            };
        }

        updateAndDraw() {
            const ctx = this.ctx;
            const w = this.canvas.width;
            const h = this.canvas.height;
            ctx.clearRect(0, 0, w, h);

            ctx.globalCompositeOperation = 'lighter';

            // Draw Flame Particles
            for (let i = 0; i < this.particles.length; i++) {
                const p = this.particles[i];
                p.life++;
                p.x += p.vx + Math.sin(p.life * p.wobbleSpeed + p.seed) * 0.8;
                p.y += p.vy;
                p.size *= 0.965;

                const progress = p.life / p.maxLife;
                if (progress >= 1 || p.size < 1) {
                    this.particles[i] = this.createFlame();
                    continue;
                }

                const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.size);
                if (progress < 0.25) {
                    grad.addColorStop(0, 'rgba(255, 255, 255, 0.95)');
                    grad.addColorStop(0.35, 'rgba(254, 240, 138, 0.85)');
                    grad.addColorStop(0.7, 'rgba(249, 115, 22, 0.5)');
                    grad.addColorStop(1, 'rgba(239, 68, 68, 0)');
                } else if (progress < 0.65) {
                    grad.addColorStop(0, 'rgba(254, 240, 138, 0.8)');
                    grad.addColorStop(0.4, 'rgba(249, 115, 22, 0.6)');
                    grad.addColorStop(0.8, 'rgba(225, 29, 72, 0.3)');
                    grad.addColorStop(1, 'rgba(159, 18, 57, 0)');
                } else {
                    grad.addColorStop(0, 'rgba(239, 68, 68, 0.45)');
                    grad.addColorStop(0.6, 'rgba(136, 19, 55, 0.2)');
                    grad.addColorStop(1, 'rgba(0, 0, 0, 0)');
                }

                ctx.fillStyle = grad;
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
                ctx.fill();
            }

            // Draw Embers
            for (let i = 0; i < this.embers.length; i++) {
                const e = this.embers[i];
                e.life++;
                e.x += e.vx + (Math.random() - 0.5) * 1.2;
                e.y += e.vy;

                const progress = e.life / e.maxLife;
                if (progress >= 1 || e.y < -10) {
                    this.embers[i] = this.createEmber();
                    continue;
                }

                const alpha = Math.sin((1 - progress) * Math.PI);
                ctx.fillStyle = e.color;
                ctx.globalAlpha = Math.max(0, alpha);
                ctx.shadowBlur = 8;
                ctx.shadowColor = '#f97316';
                ctx.beginPath();
                ctx.arc(e.x, e.y, e.size, 0, Math.PI * 2);
                ctx.fill();
            }

            ctx.globalAlpha = 1;
            ctx.shadowBlur = 0;
            ctx.globalCompositeOperation = 'source-over';
        }
    }

    // --- B. REAL BRANCHING LIGHTNING & ELECTRIC ARCS ---
    class LightningSimulator {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d', { alpha: true });
            this.bolts = [];
            this.sparks = [];
            this.lastStrike = 0;
            this.strikeInterval = 180; // ms
        }

        generateBoltPoints(x1, y1, x2, y2, displace, minDisplace = 3) {
            const points = [{ x: x1, y: y1 }, { x: x2, y: y2 }];
            let currDisplace = displace;

            while (currDisplace > minDisplace) {
                const newPoints = [];
                for (let i = 0; i < points.length - 1; i++) {
                    const p1 = points[i];
                    const p2 = points[i + 1];
                    const midX = (p1.x + p2.x) / 2;
                    const midY = (p1.y + p2.y) / 2;

                    const angle = Math.atan2(p2.y - p1.y, p2.x - p1.x) + Math.PI / 2;
                    const offset = (Math.random() - 0.5) * currDisplace * 2;

                    newPoints.push(p1);
                    newPoints.push({
                        x: midX + Math.cos(angle) * offset,
                        y: midY + Math.sin(angle) * offset
                    });
                }
                newPoints.push(points[points.length - 1]);
                points.length = 0;
                points.push(...newPoints);
                currDisplace *= 0.55;
            }
            return points;
        }

        spawnLightning() {
            const w = this.canvas.width;
            const h = this.canvas.height;
            
            // Randomly choose perimeter or cross-card arc
            const mode = Math.random();
            let x1, y1, x2, y2;

            if (mode < 0.4) {
                // Top border arc
                x1 = Math.random() * (w * 0.4);
                y1 = 2 + Math.random() * 4;
                x2 = x1 + 60 + Math.random() * (w * 0.5);
                y2 = 2 + Math.random() * 4;
            } else if (mode < 0.7) {
                // Diagonal strike
                x1 = Math.random() * w;
                y1 = Math.random() * h;
                x2 = x1 + (Math.random() - 0.5) * 120;
                y2 = y1 + (Math.random() - 0.5) * 80;
            } else {
                // Corner burst
                const corner = Math.floor(Math.random() * 4);
                x1 = corner % 2 === 0 ? 5 : w - 5;
                y1 = corner < 2 ? 5 : h - 5;
                x2 = x1 + (corner % 2 === 0 ? 1 : -1) * (40 + Math.random() * 70);
                y2 = y1 + (corner < 2 ? 1 : -1) * (30 + Math.random() * 60);
            }

            const mainPoints = this.generateBoltPoints(x1, y1, x2, y2, 22);
            const branches = [];

            // Spawn 1-2 branch forks
            if (mainPoints.length > 6 && Math.random() > 0.3) {
                const branchIdx = Math.floor(mainPoints.length * 0.4 + Math.random() * (mainPoints.length * 0.3));
                const bp = mainPoints[branchIdx];
                const bx2 = bp.x + (Math.random() - 0.5) * 50;
                const by2 = bp.y + (Math.random() - 0.5) * 50;
                branches.push(this.generateBoltPoints(bp.x, bp.y, bx2, by2, 12));
            }

            this.bolts.push({
                points: mainPoints,
                branches: branches,
                life: 0,
                maxLife: 6 + Math.floor(Math.random() * 5),
                color: Math.random() > 0.25 ? '#38bdf8' : '#facc15'
            });

            // Emit sparks at endpoints
            for (let i = 0; i < 4; i++) {
                this.sparks.push({
                    x: x2, y: y2,
                    vx: (Math.random() - 0.5) * 6,
                    vy: (Math.random() - 0.5) * 6,
                    size: 1.5 + Math.random() * 2,
                    life: 0,
                    maxLife: 15 + Math.random() * 10
                });
            }
        }

        updateAndDraw(now) {
            const ctx = this.ctx;
            const w = this.canvas.width;
            const h = this.canvas.height;
            ctx.clearRect(0, 0, w, h);

            if (now - this.lastStrike > this.strikeInterval) {
                this.spawnLightning();
                if (Math.random() > 0.4) this.spawnLightning();
                this.lastStrike = now;
                this.strikeInterval = 120 + Math.random() * 220;
            }

            ctx.globalCompositeOperation = 'lighter';

            // Draw Bolts
            for (let b = this.bolts.length - 1; b >= 0; b--) {
                const bolt = this.bolts[b];
                bolt.life++;
                const alpha = 1 - (bolt.life / bolt.maxLife);

                if (alpha <= 0) {
                    this.bolts.splice(b, 1);
                    continue;
                }

                ctx.save();
                ctx.strokeStyle = bolt.color;
                ctx.lineWidth = 2.5 * alpha;
                ctx.shadowBlur = 14;
                ctx.shadowColor = bolt.color;
                ctx.globalAlpha = alpha;

                // Main bolt path
                ctx.beginPath();
                ctx.moveTo(bolt.points[0].x, bolt.points[0].y);
                for (let i = 1; i < bolt.points.length; i++) {
                    ctx.lineTo(bolt.points[i].x, bolt.points[i].y);
                }
                ctx.stroke();

                // White core
                ctx.strokeStyle = '#ffffff';
                ctx.lineWidth = 1.2 * alpha;
                ctx.shadowBlur = 6;
                ctx.shadowColor = '#ffffff';
                ctx.stroke();

                // Branches
                for (const branch of bolt.branches) {
                    ctx.strokeStyle = bolt.color;
                    ctx.lineWidth = 1.5 * alpha;
                    ctx.beginPath();
                    ctx.moveTo(branch[0].x, branch[0].y);
                    for (let i = 1; i < branch.length; i++) {
                        ctx.lineTo(branch[i].x, branch[i].y);
                    }
                    ctx.stroke();
                }

                ctx.restore();
            }

            // Draw Sparks
            for (let s = this.sparks.length - 1; s >= 0; s--) {
                const spark = this.sparks[s];
                spark.life++;
                spark.x += spark.vx;
                spark.y += spark.vy;
                spark.vx *= 0.94;
                spark.vy *= 0.94;

                const alpha = 1 - (spark.life / spark.maxLife);
                if (alpha <= 0) {
                    this.sparks.splice(s, 1);
                    continue;
                }

                ctx.fillStyle = '#fef08a';
                ctx.globalAlpha = alpha;
                ctx.shadowBlur = 6;
                ctx.shadowColor = '#38bdf8';
                ctx.beginPath();
                ctx.arc(spark.x, spark.y, spark.size, 0, Math.PI * 2);
                ctx.fill();
            }

            ctx.globalAlpha = 1;
            ctx.shadowBlur = 0;
            ctx.globalCompositeOperation = 'source-over';
        }
    }

    // --- C. REAL TOXIC ACID & BUBBLING OOZE SIMULATOR ---
    class ToxicAcidSimulator {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d', { alpha: true });
            this.bubbles = [];
            this.drips = [];
            this.fumes = [];
            this.maxBubbles = 24;
            this.init();
        }

        init() {
            for (let i = 0; i < this.maxBubbles; i++) this.bubbles.push(this.createBubble(true));
            for (let i = 0; i < 4; i++) this.drips.push(this.createDrip(i));
        }

        createBubble(initial = false) {
            const w = this.canvas.width;
            const h = this.canvas.height;
            return {
                x: Math.random() * w,
                y: initial ? Math.random() * h : h + 10,
                vy: -0.8 - Math.random() * 1.8,
                wobbleSpeed: 0.04 + Math.random() * 0.06,
                wobbleAmp: 0.8 + Math.random() * 1.5,
                size: 3 + Math.random() * 8,
                life: initial ? Math.random() * 80 : 0,
                maxLife: 60 + Math.random() * 50,
                seed: Math.random() * 100,
                popping: false
            };
        }

        createDrip(idx) {
            const w = this.canvas.width;
            const h = this.canvas.height;
            return {
                x: w * (0.2 + idx * 0.22) + (Math.random() - 0.5) * 20,
                y: h - 2,
                length: 0,
                maxLength: 18 + Math.random() * 16,
                state: 'growing', // growing, falling, resetting
                dropY: 0,
                dropVy: 0,
                speed: 0.2 + Math.random() * 0.3
            };
        }

        updateAndDraw() {
            const ctx = this.ctx;
            const w = this.canvas.width;
            const h = this.canvas.height;
            ctx.clearRect(0, 0, w, h);

            ctx.globalCompositeOperation = 'lighter';

            // Draw Rising Toxic Bubbles
            for (let i = 0; i < this.bubbles.length; i++) {
                const b = this.bubbles[i];
                b.life++;
                b.y += b.vy;
                b.x += Math.sin(b.life * b.wobbleSpeed + b.seed) * b.wobbleAmp;

                const progress = b.life / b.maxLife;
                if (progress >= 1 || b.y < -10) {
                    this.bubbles[i] = this.createBubble();
                    continue;
                }

                const alpha = progress < 0.2 ? progress / 0.2 : progress > 0.8 ? (1 - progress) / 0.2 : 0.85;

                // Bubble glow outer ring
                ctx.save();
                ctx.strokeStyle = '#a3e635';
                ctx.lineWidth = 1.8;
                ctx.shadowBlur = 10;
                ctx.shadowColor = '#22c55e';
                ctx.globalAlpha = alpha * 0.9;
                
                ctx.beginPath();
                ctx.arc(b.x, b.y, b.size, 0, Math.PI * 2);
                ctx.stroke();

                // Translucent neon core fill
                ctx.fillStyle = 'rgba(34, 197, 94, 0.25)';
                ctx.fill();

                // Highlight glare dot
                ctx.fillStyle = '#ffffff';
                ctx.globalAlpha = alpha;
                ctx.beginPath();
                ctx.arc(b.x - b.size * 0.3, b.y - b.size * 0.3, b.size * 0.25, 0, Math.PI * 2);
                ctx.fill();
                ctx.restore();
            }

            // Draw Viscous Slime Drips along the bottom edge
            for (let i = 0; i < this.drips.length; i++) {
                const d = this.drips[i];
                ctx.save();
                ctx.fillStyle = '#a3e635';
                ctx.shadowBlur = 12;
                ctx.shadowColor = '#22c55e';

                if (d.state === 'growing') {
                    d.length += d.speed;
                    if (d.length >= d.maxLength) {
                        d.state = 'falling';
                        d.dropY = d.y + d.length;
                        d.dropVy = 1.5;
                    }
                    // Draw teardrop hanging
                    ctx.beginPath();
                    ctx.arc(d.x, d.y + d.length, 3.5, 0, Math.PI);
                    ctx.lineTo(d.x - 2, d.y);
                    ctx.lineTo(d.x + 2, d.y);
                    ctx.closePath();
                    ctx.fill();
                } else if (d.state === 'falling') {
                    d.dropY += d.dropVy;
                    d.dropVy += 0.3; // Gravity

                    // Falling drop
                    ctx.beginPath();
                    ctx.arc(d.x, d.dropY, 3, 0, Math.PI * 2);
                    ctx.fill();

                    // Stem retracting
                    d.length *= 0.85;
                    if (d.length > 2) {
                        ctx.fillRect(d.x - 1.5, d.y, 3, d.length);
                    }

                    if (d.dropY > h + 30) {
                        this.drips[i] = this.createDrip(i);
                    }
                }
                ctx.restore();
            }

            ctx.globalAlpha = 1;
            ctx.shadowBlur = 0;
            ctx.globalCompositeOperation = 'source-over';
        }
    }

    // --- D. REAL FLUID WATER & SPLASH SIMULATOR ---
    class WaterWaveSimulator {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d', { alpha: true });
            this.step = 0;
            this.droplets = [];
            this.maxDroplets = 20;
            this.init();
        }

        init() {
            for (let i = 0; i < this.maxDroplets; i++) {
                this.droplets.push(this.createDroplet(true));
            }
        }

        createDroplet(initial = false) {
            const w = this.canvas.width;
            const h = this.canvas.height;
            return {
                x: Math.random() * w,
                y: initial ? Math.random() * h : h + 10,
                vy: -0.6 - Math.random() * 1.5,
                vx: (Math.random() - 0.5) * 0.6,
                size: 2.5 + Math.random() * 6,
                life: initial ? Math.random() * 70 : 0,
                maxLife: 50 + Math.random() * 40
            };
        }

        updateAndDraw() {
            const ctx = this.ctx;
            const w = this.canvas.width;
            const h = this.canvas.height;
            ctx.clearRect(0, 0, w, h);
            this.step += 0.035;

            ctx.globalCompositeOperation = 'lighter';

            // Wave Layer 1 (Deep Blue Flow)
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(0, h);
            for (let x = 0; x <= w; x += 10) {
                const y = h * 0.72 + Math.sin(x * 0.02 + this.step) * 14 + Math.cos(x * 0.01 + this.step * 0.8) * 8;
                ctx.lineTo(x, y);
            }
            ctx.lineTo(w, h);
            ctx.closePath();
            const grad1 = ctx.createLinearGradient(0, h * 0.6, 0, h);
            grad1.addColorStop(0, 'rgba(56, 189, 248, 0.45)');
            grad1.addColorStop(1, 'rgba(99, 102, 241, 0.75)');
            ctx.fillStyle = grad1;
            ctx.shadowBlur = 12;
            ctx.shadowColor = '#38bdf8';
            ctx.fill();
            ctx.restore();

            // Wave Layer 2 (Cyan Crest Wave)
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(0, h);
            for (let x = 0; x <= w; x += 10) {
                const y = h * 0.78 + Math.sin(x * 0.025 - this.step * 1.2) * 10 + Math.cos(x * 0.03 + this.step) * 6;
                ctx.lineTo(x, y);
            }
            ctx.lineTo(w, h);
            ctx.closePath();
            ctx.fillStyle = 'rgba(147, 197, 253, 0.35)';
            ctx.fill();
            ctx.restore();

            // Floating Water Droplets / Foam Bubbles
            for (let i = 0; i < this.droplets.length; i++) {
                const d = this.droplets[i];
                d.life++;
                d.y += d.vy;
                d.x += d.vx;

                const progress = d.life / d.maxLife;
                if (progress >= 1 || d.y < -10) {
                    this.droplets[i] = this.createDroplet();
                    continue;
                }

                const alpha = Math.sin(progress * Math.PI) * 0.8;
                ctx.save();
                ctx.strokeStyle = '#93c5fd';
                ctx.lineWidth = 1.5;
                ctx.fillStyle = 'rgba(56, 189, 248, 0.2)';
                ctx.globalAlpha = alpha;
                ctx.shadowBlur = 8;
                ctx.shadowColor = '#38bdf8';
                ctx.beginPath();
                ctx.arc(d.x, d.y, d.size, 0, Math.PI * 2);
                ctx.fill();
                ctx.stroke();
                ctx.restore();
            }

            ctx.globalAlpha = 1;
            ctx.shadowBlur = 0;
            ctx.globalCompositeOperation = 'source-over';
        }
    }

    // --- E. CYBER EKG VITAL SIGNS & FLATLINE PULSE SIMULATOR ---
    class EkgMonitorSimulator {
        constructor(canvas) {
            this.canvas = canvas;
            this.ctx = canvas.getContext('2d', { alpha: true });
            this.points = [];
            this.scanX = 0;
            this.speed = 3.2;
            this.history = [];
            this.state = 'normal'; // 'normal' (green zigzag heartbeat) -> 'critical' (red flatline pulse)
            this.stateTime = performance.now();
            this.normalDuration = 6500; // 6.5s green heartbeat
            this.criticalDuration = 4500; // 4.5s red flatline pulse
            this.beatInterval = 850; // ms between heartbeat spikes
            this.lastBeat = performance.now();
            this.heartbeatPhase = 0;
        }

        generateNextY(now) {
            const h = this.canvas.height;
            const midY = h * 0.58;

            // Check State Switch
            const elapsed = now - this.stateTime;
            if (this.state === 'normal' && elapsed > this.normalDuration) {
                this.state = 'critical';
                this.stateTime = now;
                this.updateDomStatus('critical');
            } else if (this.state === 'critical' && elapsed > this.criticalDuration) {
                this.state = 'normal';
                this.stateTime = now;
                this.updateDomStatus('normal');
            }

            if (this.state === 'normal') {
                // Normal Heartbeat (P-Q-R-S-T spike complex)
                const timeSinceBeat = now - this.lastBeat;
                if (timeSinceBeat > this.beatInterval) {
                    this.lastBeat = now;
                    this.heartbeatPhase = 1;
                }

                if (this.heartbeatPhase > 0) {
                    this.heartbeatPhase++;
                    if (this.heartbeatPhase === 2) return midY - 14; // P wave bump
                    if (this.heartbeatPhase === 3) return midY + 10; // Q dip
                    if (this.heartbeatPhase === 4) return midY - (h * 0.42); // R sharp high peak
                    if (this.heartbeatPhase === 5) return midY + (h * 0.28); // S deep valley
                    if (this.heartbeatPhase === 6) return midY - 18; // T wave return
                    if (this.heartbeatPhase === 7) return midY - 8;
                    if (this.heartbeatPhase > 7) {
                        this.heartbeatPhase = 0;
                        return midY;
                    }
                }
                return midY + (Math.random() - 0.5) * 2.5; // baseline telemetry noise
            } else {
                // Critical Flatline State (Line goes flat with emergency warning pulse)
                const timeSinceBeat = now - this.lastBeat;
                if (timeSinceBeat > 1200) {
                    this.lastBeat = now;
                    this.heartbeatPhase = 1;
                }

                if (this.heartbeatPhase > 0) {
                    this.heartbeatPhase++;
                    if (this.heartbeatPhase === 2) return midY - (h * 0.35); // Sharp warning alarm pulse
                    if (this.heartbeatPhase === 3) return midY + (h * 0.22);
                    if (this.heartbeatPhase > 3) {
                        this.heartbeatPhase = 0;
                        return midY;
                    }
                }
                // Flatline with micro vibration
                return midY + (Math.random() - 0.5) * 1.5;
            }
        }

        updateDomStatus(state) {
            const badge = document.querySelector('.hero-ekg-status');
            if (!badge) return;
            if (state === 'normal') {
                badge.className = 'hero-ekg-status px-3.5 py-1 rounded-full text-xs font-black bg-emerald-500/20 text-emerald-300 border border-emerald-400/40 backdrop-blur-md flex items-center gap-2 shadow-xs transition-all duration-300';
                badge.innerHTML = '<span class="w-2 h-2 rounded-full bg-emerald-400 animate-ping"></span><span class="font-mono">💚 VITALS: STABLE [78 BPM]</span>';
            } else {
                badge.className = 'hero-ekg-status px-3.5 py-1 rounded-full text-xs font-black bg-rose-500/30 text-rose-300 border border-rose-400/60 backdrop-blur-md flex items-center gap-2 shadow-lg shadow-rose-500/30 animate-pulse transition-all duration-300';
                badge.innerHTML = '<span class="w-2 h-2 rounded-full bg-rose-500 animate-ping"></span><span class="font-mono">🚨 CRITICAL FLATLINE [00 BPM]</span>';
            }
        }

        updateAndDraw(now) {
            const ctx = this.ctx;
            const w = this.canvas.width;
            const h = this.canvas.height;

            // Advance Scanhead
            this.scanX = (this.scanX + this.speed) % w;
            const newY = this.generateNextY(now);
            this.history.push({ x: this.scanX, y: newY, time: now, state: this.state });

            // Clear old history
            if (this.history.length > w * 1.6) {
                this.history.shift();
            }

            ctx.clearRect(0, 0, w, h);

            const isNormal = this.state === 'normal';
            const strokeColor = isNormal ? '#22c55e' : '#ef4444';
            const glowColor = isNormal ? '#4ade80' : '#f43f5e';

            // Draw Ambient Pulse Glow in the whole card background
            ctx.save();
            const grad = ctx.createRadialGradient(this.scanX, h * 0.58, 0, this.scanX, h * 0.58, 260);
            grad.addColorStop(0, isNormal ? 'rgba(34, 197, 94, 0.12)' : 'rgba(239, 68, 68, 0.16)');
            grad.addColorStop(1, 'rgba(0, 0, 0, 0)');
            ctx.fillStyle = grad;
            ctx.fillRect(0, 0, w, h);
            ctx.restore();

            // Draw Subtle Cyber Telemetry Grid
            ctx.save();
            ctx.strokeStyle = 'rgba(99, 102, 241, 0.06)';
            ctx.lineWidth = 1;
            for (let x = 0; x < w; x += 35) {
                ctx.beginPath();
                ctx.moveTo(x, 0);
                ctx.lineTo(x, h);
                ctx.stroke();
            }
            for (let y = 0; y < h; y += 28) {
                ctx.beginPath();
                ctx.moveTo(0, y);
                ctx.lineTo(w, y);
                ctx.stroke();
            }
            ctx.restore();

            // Draw EKG Signal Path
            if (this.history.length < 2) return;

            ctx.save();
            ctx.strokeStyle = strokeColor;
            ctx.lineWidth = 2.8;
            ctx.shadowBlur = 16;
            ctx.shadowColor = glowColor;
            ctx.lineCap = 'round';
            ctx.lineJoin = 'round';

            for (let i = 1; i < this.history.length; i++) {
                const p1 = this.history[i - 1];
                const p2 = this.history[i];

                // Don't draw across wrap-around seam
                if (Math.abs(p2.x - p1.x) > 25) continue;

                // Fade tail
                const age = now - p2.time;
                const alpha = Math.max(0, 1 - (age / 4500));
                ctx.globalAlpha = alpha;

                ctx.beginPath();
                ctx.moveTo(p1.x, p1.y);
                ctx.lineTo(p2.x, p2.y);
                ctx.stroke();
            }

            // Draw Leading Scanner Dot / Laser Pulse Blip
            const latest = this.history[this.history.length - 1];
            if (latest) {
                ctx.globalAlpha = 1;
                ctx.fillStyle = '#ffffff';
                ctx.shadowBlur = 22;
                ctx.shadowColor = glowColor;
                ctx.beginPath();
                ctx.arc(latest.x, latest.y, 4.5, 0, Math.PI * 2);
                ctx.fill();

                // Vertical scanline guide
                ctx.strokeStyle = strokeColor;
                ctx.lineWidth = 1.2;
                ctx.globalAlpha = 0.3;
                ctx.beginPath();
                ctx.moveTo(latest.x, 0);
                ctx.lineTo(latest.x, h);
                ctx.stroke();
            }

            ctx.restore();
        }
    }

    // =========================================================================
    // 2. VFX CONTROLLER & AUTO-MOUNTING
    // =========================================================================

    const activeSimulators = new Map();

    function initElementalCanvases() {
        const canvases = document.querySelectorAll('.element-vfx-canvas');
        canvases.forEach(canvas => {
            if (activeSimulators.has(canvas)) return;

            const element = canvas.getAttribute('data-element') || 'none';
            const rect = canvas.getBoundingClientRect();
            canvas.width = Math.max(rect.width, 240);
            canvas.height = Math.max(rect.height, 140);

            let sim = null;
            if (element === 'fire') sim = new FireSimulator(canvas);
            else if (element === 'lightning') sim = new LightningSimulator(canvas);
            else if (element === 'toxic') sim = new ToxicAcidSimulator(canvas);
            else if (element === 'water') sim = new WaterWaveSimulator(canvas);

            if (sim) {
                activeSimulators.set(canvas, sim);
            }
        });

        // Initialize EKG Canvases (Full Card Background Sizing)
        const ekgCanvases = document.querySelectorAll('.hero-ekg-canvas');
        ekgCanvases.forEach(canvas => {
            if (activeSimulators.has(canvas)) return;
            const parent = canvas.parentElement || canvas;
            const rect = parent.getBoundingClientRect();
            canvas.width = Math.max(rect.width, 600);
            canvas.height = Math.max(rect.height, 220);
            activeSimulators.set(canvas, new EkgMonitorSimulator(canvas));
        });
    }

    // Main 60 FPS Render Loop
    function renderVfxLoop(now) {
        activeSimulators.forEach((sim, canvas) => {
            if (!document.body.contains(canvas)) {
                activeSimulators.delete(canvas);
                return;
            }
            sim.updateAndDraw(now);
        });
        requestAnimationFrame(renderVfxLoop);
    }
    requestAnimationFrame(renderVfxLoop);

    // =========================================================================
    // 3. 3D TILT & INTERACTION HOOKS
    // =========================================================================

    function init3DTilt() {
        const cards = document.querySelectorAll('.gamer-card-3d:not([data-tilt-initialized])');
        cards.forEach(card => {
            card.setAttribute('data-tilt-initialized', 'true');

            card.addEventListener('mousemove', (e) => {
                const rect = card.getBoundingClientRect();
                const x = e.clientX - rect.left;
                const y = e.clientY - rect.top;
                
                const centerX = rect.width / 2;
                const centerY = rect.height / 2;
                
                const rotateX = ((y - centerY) / centerY) * -9; // Max tilt 9deg
                const rotateY = ((x - centerX) / centerX) * 9;

                card.style.transform = `perspective(1000px) rotateX(${rotateX.toFixed(2)}deg) rotateY(${rotateY.toFixed(2)}deg) translateY(-5px) scale(1.03)`;
                
                const glareX = (x / rect.width) * 100;
                const glareY = (y / rect.height) * 100;
                card.style.setProperty('--glare-x', `${glareX.toFixed(1)}%`);
                card.style.setProperty('--glare-y', `${glareY.toFixed(1)}%`);
            });

            card.addEventListener('mouseleave', () => {
                card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) translateY(0) scale(1)';
            });
        });
    }

    // --- 4. Digital Odometer / Smooth Counter Roll ---
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

    // --- 5. Spark Burst Trigger ---
    window.triggerSparkBurst = function (element, color = '#6366f1') {
        if (!element) return;
        const rect = element.getBoundingClientRect();
        const count = 22;
        
        for (let i = 0; i < count; i++) {
            const spark = document.createElement('div');
            spark.className = 'gamer-particle-spark';
            spark.style.backgroundColor = color;
            spark.style.boxShadow = `0 0 10px ${color}, 0 0 20px ${color}`;
            
            const startX = rect.left + rect.width / 2;
            const startY = rect.top + rect.height / 2;
            spark.style.left = `${startX}px`;
            spark.style.top = `${startY}px`;
            
            const angle = (Math.PI * 2 * i) / count + (Math.random() - 0.5);
            const velocity = 50 + Math.random() * 80;
            const destX = Math.cos(angle) * velocity;
            const destY = Math.sin(angle) * velocity;
            
            spark.style.setProperty('--dest-x', `${destX}px`);
            spark.style.setProperty('--dest-y', `${destY}px`);
            
            document.body.appendChild(spark);
            setTimeout(() => spark.remove(), 700);
        }
    };

    function initAll() {
        initElementalCanvases();
        init3DTilt();
        initCounters();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAll);
    } else {
        initAll();
    }

    // Dynamic Observer for Blazor circuit DOM changes
    const observer = new MutationObserver(() => {
        initAll();
    });

    observer.observe(document.body, { childList: true, subtree: true });

    // Handle Window Resize for Canvases
    window.addEventListener('resize', () => {
        activeSimulators.forEach((sim, canvas) => {
            const rect = canvas.getBoundingClientRect();
            canvas.width = rect.width;
            canvas.height = rect.height;
        });
    });

    window.initGamerHud = initAll;
})();
