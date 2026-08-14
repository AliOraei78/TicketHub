/**
 * Lightweight Matrix Digital Rain Canvas VFX for TicketHub System Logs
 */
window.matrixRain = {
    animationId: null,
    canvas: null,
    ctx: null,
    columns: 0,
    drops: [],
    characters: '0123456789ABCDEF01010101XYZアイウエオカキクケコサシスセソタチツテト',

    init: function (canvasId) {
        this.destroy();
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) return;

        this.canvas.style.position = 'fixed';
        this.canvas.style.top = '0';
        this.canvas.style.left = '0';
        this.canvas.style.width = '100vw';
        this.canvas.style.height = '100vh';
        this.canvas.style.maxWidth = '100vw';
        this.canvas.style.maxHeight = '100vh';
        this.canvas.style.pointerEvents = 'none';
        this.canvas.style.zIndex = '0';

        this.ctx = this.canvas.getContext('2d');
        if (!this.ctx) return;

        this.resize();
        window.addEventListener('resize', this.handleResize);

        var fontSize = 14;
        this.columns = Math.floor(this.canvas.width / fontSize);
        this.drops = [];
        for (var i = 0; i < this.columns; i++) {
            this.drops[i] = Math.floor(Math.random() * -100);
        }

        var self = this;
        var lastTime = 0;
        var fps = 25; // Smooth but low CPU usage
        var interval = 1000 / fps;

        function draw(time) {
            self.animationId = requestAnimationFrame(draw);
            var delta = time - lastTime;
            if (delta < interval) return;
            lastTime = time - (delta % interval);

            // Semi-transparent black background to create trail effect
            self.ctx.fillStyle = 'rgba(2, 8, 4, 0.08)';
            self.ctx.fillRect(0, 0, self.canvas.width, self.canvas.height);

            self.ctx.fillStyle = '#00ff66';
            self.ctx.font = fontSize + 'px monospace';

            for (var i = 0; i < self.drops.length; i++) {
                var char = self.characters.charAt(Math.floor(Math.random() * self.characters.length));
                var x = i * fontSize;
                var y = self.drops[i] * fontSize;

                // Randomly draw some leading characters brighter
                if (Math.random() > 0.85) {
                    self.ctx.fillStyle = '#bbf7d0'; // Light bright green head
                    self.ctx.fillText(char, x, y);
                    self.ctx.fillStyle = '#00ff66'; // Reset back to matrix green
                } else {
                    self.ctx.fillText(char, x, y);
                }

                if (y > self.canvas.height && Math.random() > 0.975) {
                    self.drops[i] = 0;
                }
                self.drops[i]++;
            }
        }

        this.animationId = requestAnimationFrame(draw);
    },

    resize: function () {
        if (!this.canvas) return;
        this.canvas.width = document.documentElement.clientWidth || window.innerWidth;
        this.canvas.height = document.documentElement.clientHeight || window.innerHeight;
        var fontSize = 14;
        this.columns = Math.floor(this.canvas.width / fontSize);
        if (this.drops.length !== this.columns) {
            this.drops = [];
            for (var i = 0; i < this.columns; i++) {
                this.drops[i] = Math.floor(Math.random() * -50);
            }
        }
    },

    handleResize: function () {
        if (window.matrixRain) {
            window.matrixRain.resize();
        }
    },

    destroy: function () {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
            this.animationId = null;
        }
        window.removeEventListener('resize', this.handleResize);
        this.canvas = null;
        this.ctx = null;
        this.drops = [];
    }
};
