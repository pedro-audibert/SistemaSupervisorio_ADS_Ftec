# Sistema Supervisório MMDBA

**Projeto de Conclusão de Curso em Análise e Desenvolvimento de Sistemas.**

   

**Status do Projeto:** `Em Andamento`

-----

## 📄 Sobre o Projeto

O Sistema Supervisório MMDBA foi desenvolvido como uma plataforma integrada de supervisão e gestão para pequenas e médias empresas (PMEs). O foco é fornecer uma solução acessível, escalável e com dados acionáveis em tempo real para gestores de produção e equipes de manutenção.

O sistema atende à necessidade crítica de informações em tempo real sobre o status de equipamentos industriais, facilitando a tomada de decisões estratégicas, a manutenção proativa e a melhoria da eficiência operacional. A plataforma é acessível via navegador em desktops e dispositivos móveis.

## ✨ Funcionalidades

A arquitetura do sistema foi dividida em três pilares principais:

#### 🏛️ **Pilar I: Plataforma e Aquisição de Dados**

A fundação do sistema, responsável pela segurança, conectividade e coleta de dados brutos do chão de fábrica.

  * **[RF1] Login e Segurança:** Módulo completo construído sobre **ASP.NET Core Identity**, garantindo acesso autorizado com registro, login seguro, confirmação obrigatória de e-mail, recuperação de senha, suporte para 2FA e gestão de dados pessoais (LGPD).
  * **[RF2] Coleta de Dados (IIoT):** Utiliza **Node-RED** como gateway para se conectar a CLPs, ler dados de sensores/atuadores, formatá-los em JSON e enviá-los de forma segura para a API central.

#### ⚙️ **Pilar II: Operação e Supervisão em Tempo Real**

Ferramentas para o monitoramento diário e reação imediata ao estado da produção.

  * **[RF3] Painel de Diagnóstico de I/O:** Uma tela de baixo nível para manutenção, exibindo em tempo real (via **SignalR**) o estado de todas as entradas e saídas do CLP.
  * **[RF4] Histórico de Eventos:** Tela para análise e rastreabilidade de todos os eventos operacionais (Alarmes, Avisos, Status) com filtros e pesquisa.
  * **[RF5] Dashboard de Produção:** A tela principal do sistema, com KPIs, gauge de velocidade e gráficos de tendência em **janela deslizante da última hora**, projetados para serem robustos e nunca ficarem vazios.

#### 📊 **Pilar III: Gestão e Análise Estratégica**

Transforma dados brutos em inteligência de negócio.

  * **[RF6] Emissão de Relatórios:** Módulo para consolidar dados históricos em relatórios de alarmes, tempo de parada e produtividade.
  * **[RF7] Notificações Proativas:** Envio de alertas automáticos via E-mail (com **SendGrid**) e WhatsApp para eventos críticos.
  * **[RF8] Análise de OEE:** Cálculo e visualização do indicador de eficiência OEE (Disponibilidade, Performance e Qualidade).
  * **[RF9] Gestão de Usuários:** Painel administrativo para gerenciar usuários e atribuir papéis (Administrador, Operador, etc.).

## 🏗️ Arquitetura e Fluxo de Dados

O fluxo de dados é linear e desacoplado, garantindo manutenibilidade e escalabilidade.

1.  **Fonte de Dados (CLP):** Gera dados brutos (status de I/Os, contagem, velocidade).
2.  **Coleta (Node-RED):** Atua como gateway, formata os dados em JSON e os envia via `HTTP POST` para o Backend.
3.  **Processamento (Backend - API):**
      * A API recebe a requisição JSON.
      * O timestamp é atribuído no momento do recebimento usando `DateTime.UtcNow` para garantir consistência universal.
      * Os dados são persistidos no banco de dados PostgreSQL.
4.  **Transmissão (Backend - SignalR):** Após salvar no banco, a API transmite o novo dado via SignalR para os clientes conectados.
5.  **Visualização (Frontend - Dashboard):**
      * **Carga Inicial:** A página busca via `HTTP GET` um conjunto otimizado de dados históricos (última hora + ponto de âncora) para garantir que os gráficos sempre carreguem preenchidos.
      * **Atualização Contínua:** A conexão WebSocket com SignalR recebe novos dados e atualiza a interface dinamicamente.

### Otimização de Performance

Para garantir a carga quase instantânea do dashboard, foram implementados **índices estratégicos** nas colunas de `Timestamp` e `TipoEvento` no banco de dados PostgreSQL, eliminando a necessidade de varreduras completas da tabela (`full table scans`).

## 💻 Pilha Tecnológica

  * **Backend:** C\# com ASP.NET Core 8 (API RESTful, Razor Pages, SignalR)
  * **Banco de Dados:** PostgreSQL
  * **ORM:** Entity Framework Core 8
  * **Frontend:** JavaScript (ES6+), Chart.js 4.x, Bootstrap
  * **Gateway IIoT:** Node-RED
  * **Segurança:** ASP.NET Core Identity
  * **Envio de Email:** SendGrid

    

## 👨‍💻 Autor

**Pedro Audibert Junior**

  * [GitHub](https://www.google.com/search?q=https://github.com/pedroaudibert)
  * [LinkedIn](https://www.linkedin.com/in/pedroaudibert/)
