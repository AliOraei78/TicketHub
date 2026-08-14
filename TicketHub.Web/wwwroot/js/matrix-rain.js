/**
 * Lightweight Continuous Matrix Digital Rain Canvas VFX for TicketHub
 */
window.matrixRain = {
    animationId: null,
    canvas: null,
    ctx: null,
    columns: 0,
    drops: [],
    characters: '0123456789ABCDEF01010101XYZｦｱｳｴｵｶｷｹｺｻｼｽｾｿﾀﾂﾃﾅﾆﾇﾈﾊﾋﾎﾏﾐﾑﾒﾓﾔﾕﾗﾘﾜ',

    init: function (canvasId) {
        this.destroy();
        this.canvas = document.getElementById(canvasId);
        if (!this.canvas) return;

        this.ctx = this.canvas.getContext('2d');
        if (!this.ctx) return;

        this.resize();
        window.addEventListener('resize', this.handleResize);

        var fontSize = 15;
        this.columns = Math.floor(this.canvas.width / fontSize);
        this.drops = [];
        var maxRows = Math.floor(this.canvas.height / fontSize);

        // Pre-fill drops across the entire height so rain is constantly and seamlessly flowing
        for (var i = 0; i < this.columns; i++) {
            this.drops[i] = Math.floor(Math.random() * maxRows);
        }

        var self = this;
        var lastTime = 0;
        var fps = 30;
        var interval = 1000 / fps;

        function draw(time) {
            self.animationId = requestAnimationFrame(draw);
            var delta = time - lastTime;
            if (delta < interval) return;
            lastTime = time - (delta % interval);

            // Semi-transparent overlay to create continuous decaying phosphor stream trail
            self.ctx.fillStyle = 'rgba(3, 7, 18, 0.12)';
            self.ctx.fillRect(0, 0, self.canvas.width, self.canvas.height);

            self.ctx.font = 'bold ' + fontSize + 'px monospace';

            for (var i = 0; i < self.drops.length; i++) {
                var char = self.characters.charAt(Math.floor(Math.random() * self.characters.length));
                var x = i * fontSize;
                var y = self.drops[i] * fontSize;

                // Bright leading glow char
                if (Math.random() > 0.75) {
                    self.ctx.fillStyle = '#ffffff';
                    self.ctx.shadowColor = '#00ff66';
                    self.ctx.shadowBlur = 8;
                    self.ctx.fillText(char, x, y);
                    self.ctx.shadowBlur = 0;
                } else {
                    self.ctx.fillStyle = '#10b981';
                    self.ctx.fillText(char, x, y);
                }

                // Seamless continuous reset without all columns pausing or grouping together
                if (y > self.canvas.height) {
                    if (Math.random() > 0.96 || y > self.canvas.height + 150) {
                        self.drops[i] = Math.floor(Math.random() * -15);
                    }
                }
                self.drops[i]++;
            }
        }

        this.animationId = requestAnimationFrame(draw);
    },

    resize: function () {
        if (!this.canvas) return;
        this.canvas.width = window.innerWidth;
        this.canvas.height = window.innerHeight;
        var fontSize = 15;
        this.columns = Math.floor(this.canvas.width / fontSize);
        var maxRows = Math.floor(this.canvas.height / fontSize);
        if (this.drops.length !== this.columns) {
            this.drops = [];
            for (var i = 0; i < this.columns; i++) {
                this.drops[i] = Math.floor(Math.random() * maxRows);
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
        if (this.ctx && this.canvas) {
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        }
    }
};
