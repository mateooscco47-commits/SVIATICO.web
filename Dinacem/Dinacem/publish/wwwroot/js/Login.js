document.addEventListener('DOMContentLoaded', () => {
    // 1. Mostrar / Ocultar Contraseña
    const passwordInput = document.getElementById('passwordInput');
    const btnTogglePassword = document.getElementById('btnTogglePassword');

    if (btnTogglePassword && passwordInput) {
        btnTogglePassword.addEventListener('click', () => {
            const isPassword = passwordInput.type === 'password';
            passwordInput.type = isPassword ? 'text' : 'password';
            btnTogglePassword.querySelector('i').className = isPassword ? 'bi bi-eye-slash-fill' : 'bi bi-eye-fill';
        });
    }

    // 2. Procesar Login
    const loginForm = document.getElementById('loginForm');
    const btnSubmit = document.getElementById('btnSubmit');
    const welcomeModalEl = document.getElementById('welcomeModal');
    const errorModalEl = document.getElementById('errorModal');

    if (loginForm && welcomeModalEl && errorModalEl) {
        const welcomeModal = new bootstrap.Modal(welcomeModalEl);
        const errorModal = new bootstrap.Modal(errorModalEl);

        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const formData = new FormData(loginForm);

            btnSubmit.disabled = true;
            btnSubmit.innerHTML = `<span class="spinner-border spinner-border-sm me-2" role="status"></span>Verificando...`;

            try {
                const response = await fetch(loginForm.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                const data = await response.json();

                if (data.success) {
                    // DATOS CORRECTOS
                    document.getElementById('welcomeUserText').textContent = `¡Bienvenido, ${data.nombre}! Acceso concedido.`;
                    welcomeModal.show();

                    setTimeout(() => {
                        window.location.href = data.redirectUrl;
                    }, 1200);
                } else {
                    // DATOS INCORRECTOS O ERROR DE VALIDACIÓN
                    document.getElementById('errorModalText').textContent = data.message;
                    errorModal.show();

                    btnSubmit.disabled = false;
                    btnSubmit.innerHTML = 'Ingresar';
                }
            } catch (error) {
                document.getElementById('errorModalText').textContent = "Ocurrió un error al procesar la solicitud.";
                errorModal.show();

                btnSubmit.disabled = false;
                btnSubmit.innerHTML = 'Ingresar';
            }
        });
    }

    // 3. Animación de Fondo Dinámica
    const canvas = document.getElementById('bgCanvas');
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    let width = canvas.width = window.innerWidth;
    let height = canvas.height = window.innerHeight;

    window.addEventListener('resize', () => {
        width = canvas.width = window.innerWidth;
        height = canvas.height = window.innerHeight;
    });

    const particles = [];
    const particleCount = 45;

    for (let i = 0; i < particleCount; i++) {
        particles.push({
            x: Math.random() * width,
            y: Math.random() * height,
            vx: (Math.random() - 0.5) * 0.8,
            vy: (Math.random() - 0.5) * 0.8,
            radius: Math.random() * 2 + 1.5
        });
    }

    function animate() {
        ctx.clearRect(0, 0, width, height);

        const gradient = ctx.createLinearGradient(0, 0, width, height);
        gradient.addColorStop(0, '#07152b');
        gradient.addColorStop(1, '#0c2752');
        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, width, height);

        for (let i = 0; i < particleCount; i++) {
            let p = particles[i];
            p.x += p.vx;
            p.y += p.vy;

            if (p.x < 0 || p.x > width) p.vx *= -1;
            if (p.y < 0 || p.y > height) p.vy *= -1;

            ctx.beginPath();
            ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2);
            ctx.fillStyle = 'rgba(123, 179, 66, 0.6)';
            ctx.fill();

            for (let j = i + 1; j < particleCount; j++) {
                let p2 = particles[j];
                let dist = Math.hypot(p.x - p2.x, p.y - p2.y);
                if (dist < 140) {
                    ctx.beginPath();
                    ctx.moveTo(p.x, p.y);
                    ctx.lineTo(p2.x, p2.y);
                    ctx.strokeStyle = `rgba(12, 62, 138, ${1 - dist / 140})`;
                    ctx.lineWidth = 0.8;
                    ctx.stroke();
                }
            }
        }
        requestAnimationFrame(animate);
    }

    animate();
});