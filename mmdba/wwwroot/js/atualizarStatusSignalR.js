async function iniciarHubSignalR(hubUrl, statusElement, callback) {
    if (!statusElement) return;

    const hub = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    const atualizarStatusVisual = (texto, classe) => {
        statusElement.textContent = texto;
        statusElement.className = `alert ${classe} alert-status text-center`;
    };

    hub.onreconnecting(error => {
        atualizarStatusVisual("Reconectando ao servidor...", "alert-warning");
        console.warn("SignalR reconectando:", error);
    });

    hub.onreconnected(connectionId => {
        atualizarStatusVisual("Reconectado ao servidor", "alert-success");
        console.info("SignalR reconectado. ID:", connectionId);
    });

    hub.onclose(error => {
        atualizarStatusVisual("Conexão perdida", "alert-danger");
        console.error("SignalR conexão fechada:", error);
    });

    if (callback) {
        const eventName = hubUrl.split("/").pop();
        hub.on(eventName, callback);
    }

    atualizarStatusVisual("Conectando ao servidor...", "alert-info");

    try {
        await hub.start();
        atualizarStatusVisual("Conectado ao servidor em " + new Date().toLocaleTimeString(), "alert-primary");
    } catch (err) {
        atualizarStatusVisual("Falha ao conectar", "alert-danger");
        console.error("Erro ao iniciar SignalR:", err);
    }

    return hub;
}
