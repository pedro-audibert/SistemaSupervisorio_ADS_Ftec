// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// =========================================================================
// FUNÇÃO GLOBAL PARA EXIBIR NOTIFICAÇÕES (MODAL)
// =========================================================================
function exibirModalAlerta(mensagem, titulo = 'Atenção') {
    const modalTituloEl = document.getElementById('modalAlertaLabel');
    const modalCorpoEl = document.getElementById('modalAlertaCorpo');
    const modalEl = document.getElementById('modalAlerta');

    if (modalTituloEl && modalCorpoEl && modalEl) {
        // Define o título e o corpo da mensagem
        modalTituloEl.innerText = titulo;
        modalCorpoEl.innerText = mensagem;

        // Cria uma instância do modal do Bootstrap e a exibe
        const meuModal = new bootstrap.Modal(modalEl);
        meuModal.show();
    }
}
