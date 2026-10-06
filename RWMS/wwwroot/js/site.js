const burger = document.querySelector('.burger');
const backdrop = document.querySelector('.sidebar-backdrop');

if (burger) {
    const stored = localStorage.getItem('sidebar-collapsed');
    if (stored === 'true' || (stored === null && window.innerWidth <= 1280)) {
        document.body.classList.add('sidebar-collapsed');
    }

    burger.addEventListener('click', function () {
        document.body.classList.toggle('sidebar-collapsed');
        localStorage.setItem('sidebar-collapsed', document.body.classList.contains('sidebar-collapsed'));
    });

    if (backdrop) {
        backdrop.addEventListener('click', function () {
            document.body.classList.add('sidebar-collapsed');
            localStorage.setItem('sidebar-collapsed', 'true');
        });
    }
}
