const DEFAULT_TICKET_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

const backToRegistry = document.getElementById("back-to-registry");
const navCurrentCard = document.getElementById("nav-current-card");
const statusBanner = document.getElementById("card-status-banner");

const ticketNumberEl = document.getElementById("ticket-number");
const ticketStatusEl = document.getElementById("ticket-status");
const ticketMetaEl = document.getElementById("ticket-meta");
const ticketTitleEl = document.getElementById("ticket-title");
const windowTitleEl = document.getElementById("window-title");

const stepsBox = document.getElementById("ticket-steps");
const attachmentsBox = document.getElementById("ticket-attachments");
const historyBox = document.getElementById("ticket-history");
const commentsTitle = document.getElementById("comments-title");
const commentsBox = document.getElementById("ticket-comments");

const commentForm = document.getElementById("comment-form");
const commentTextInput = document.getElementById("comment-text");
const commentFilesInput = document.getElementById("comment-files");
const selectedFilesNote = document.getElementById("selected-files-note");

const assigneeBox = document.getElementById("ticket-assignee-box");
const priorityBox = document.getElementById("ticket-priority-box");
const deadlineBox = document.getElementById("ticket-deadline-box");
const statusActionsTitle = document.getElementById("status-actions-title");
const statusActionsBox = document.getElementById("status-actions-box");

const assignDialog = document.getElementById("assign-dialog");
const assignForm = document.getElementById("assign-form");
const assignMemberSelect = document.getElementById("assign-member-select");

const reviewDialog = document.getElementById("review-dialog");
const reviewForm = document.getElementById("review-form");

let currentTicketId = getQueryParam("ticketId") || getQueryParam("ticket") || DEFAULT_TICKET_ID;
let currentTicket = null;

bindDialog(
    assignDialog,
    document.getElementById("open-assign-dialog"),
    [
        document.getElementById("close-assign-dialog"),
        document.getElementById("cancel-assign-dialog")
    ]
);

bindDialog(
    reviewDialog,
    null,
    [
        document.getElementById("close-review-dialog"),
        document.getElementById("cancel-review-dialog")
    ]
);

function showBanner(message) {
    statusBanner.textContent = message;
    statusBanner.hidden = !message;
}

function renderMultilineLines(container, text, emptyText) {
    const lines = (text || "").split(/\r?\n/).filter((line) => line.trim() !== "");
    if (lines.length === 0) {
        container.innerHTML = `<p class="detail-line">${escapeHtml(emptyText)}</p>`;
        return;
    }
    container.innerHTML = lines
        .map((line) => `<p class="detail-line">${escapeHtml(line)}</p>`)
        .join("");
}

function renderStatusActions(ticket) {
    const statusLabel = STATUS_LABELS[ticket.status] || ticket.status;
    statusActionsTitle.textContent = `ДЕЙСТВИЯ ПО СТАТУСУ «${statusLabel.toUpperCase()}»:`;

    if (ticket.status === "New" || ticket.status === "InProgress") {
        statusActionsBox.innerHTML = `
            <p class="detail-line">После исправления отправьте тикет на проверку тестировщику.</p>
            <button class="button" id="btn-open-review" type="button">Отправить на ревью</button>
        `;
        document.getElementById("btn-open-review").addEventListener("click", () => {
            reviewDialog.showModal();
        });
        return;
    }

    if (ticket.status === "OnReview") {
        statusActionsBox.innerHTML = `
            <p class="detail-line">Проверьте исправление и подтвердите закрытие или верните в работу.</p>
            <div class="row-actions">
                <button class="button" id="btn-verify-approve" type="button">Подтвердить</button>
                <button class="button dark" id="btn-verify-reject" type="button">На доработку</button>
            </div>
        `;
        document.getElementById("btn-verify-approve").addEventListener("click", () => verifyTicket(true));
        document.getElementById("btn-verify-reject").addEventListener("click", () => verifyTicket(false));
        return;
    }

    statusActionsBox.innerHTML = '<p class="detail-line">Тикет закрыт и больше не изменяется.</p>';
}

function renderTicket(ticket) {
    currentTicket = ticket;

    const statusLabel = STATUS_LABELS[ticket.status] || ticket.status;
    ticketNumberEl.textContent = `#BUG-${ticket.ticketNumber}`;
    ticketStatusEl.textContent = statusLabel;
    ticketMetaEl.textContent = `Автор: ${ticket.authorName} · ${ticket.discoveryVersion}`;
    ticketTitleEl.textContent = ticket.title;
    windowTitleEl.textContent = `#BUG-${ticket.ticketNumber} — ${ticket.title}`;

    const projectId = getQueryParam("projectId") || ticket.projectId;
    backToRegistry.href = `SCREEN_TICKET_REGISTRY.html?projectId=${encodeURIComponent(projectId)}`;
    navCurrentCard.href = `SCREEN_TICKET_CARD.html?ticketId=${encodeURIComponent(ticket.id)}&projectId=${encodeURIComponent(projectId)}`;

    renderMultilineLines(stepsBox, ticket.reproductionSteps, "Шаги воспроизведения не указаны.");

    attachmentsBox.innerHTML = (ticket.attachments || []).length > 0
        ? ticket.attachments.map((file) => `<p class="detail-line">📎 ${escapeHtml(file)}</p>`).join("")
        : '<p class="detail-line">Вложений нет.</p>';

    historyBox.innerHTML = (ticket.history || []).length > 0
        ? ticket.history.map((item) => `
            <p class="detail-line">${escapeHtml(formatDateTime(item.timestamp))} · ${escapeHtml(item.performedByName)}: ${escapeHtml(item.eventDescription)}</p>
        `).join("")
        : '<p class="detail-line">История пока пуста.</p>';

    const comments = ticket.comments || [];
    commentsTitle.textContent = `ОБСУЖДЕНИЕ (${comments.length}):`;
    commentsBox.innerHTML = comments.length > 0
        ? comments.map((c) => {
            const files = (c.attachments || []).length > 0
                ? `<p class="detail-line">📎 ${c.attachments.map(escapeHtml).join(", ")}</p>`
                : "";
            return `
                <p class="detail-line"><strong>${escapeHtml(c.authorName)}</strong> · ${escapeHtml(formatDateTime(c.createdAt))}</p>
                <p class="detail-line">${escapeHtml(c.text)}</p>
                ${files}
            `;
        }).join("")
        : '<p class="detail-line">Комментариев пока нет.</p>';

    assigneeBox.innerHTML = `<p class="detail-line">${escapeHtml(ticket.assigneeName || "Не назначен")}</p>`;
    priorityBox.innerHTML = `<p class="detail-line">${escapeHtml(ticket.priority)}</p>`;
    deadlineBox.innerHTML = `<p class="detail-line">${escapeHtml(formatDate(ticket.deadline))}</p>`;

    renderStatusActions(ticket);
}

async function loadMembersForAssign(projectId) {
    const members = (await apiRequest(`/projects/${encodeURIComponent(projectId)}/members`)) || [];
    const options = members.map((m) =>
        `<option value="${escapeHtml(m.userId)}">${escapeHtml(m.fullName)} (@${escapeHtml(m.username)})</option>`
    ).join("");
    assignMemberSelect.innerHTML = `<option value="">Снять исполнителя (Новый)</option>${options}`;
    if (currentTicket?.assigneeId) {
        assignMemberSelect.value = currentTicket.assigneeId;
    }
}

async function loadTicket() {
    const ticket = await apiRequest(`/tickets/${encodeURIComponent(currentTicketId)}`);
    renderTicket(ticket);
    await loadMembersForAssign(ticket.projectId);
}

async function verifyTicket(approved) {
    let comment = "Проверено, исправлено";
    if (!approved) {
        const input = window.prompt("Укажите комментарий при возврате на доработку:", "Ошибка все еще воспроизводится");
        if (input === null) {
            return;
        }
        comment = input.trim() || "Возвращено на доработку";
    }

    try {
        const result = await apiRequest(`/tickets/${encodeURIComponent(currentTicketId)}/verify`, {
            method: "POST",
            body: JSON.stringify({ approved, comment })
        });
        showBanner(result?.message || "");
        await loadTicket();
    } catch (error) {
        alert(error.message);
    }
}

commentFilesInput.addEventListener("change", () => {
    const names = Array.from(commentFilesInput.files || []).map((f) => f.name);
    selectedFilesNote.textContent = names.length > 0
        ? `Выбраны файлы: ${names.join(", ")}`
        : "Имена выбранных файлов передаются строкой (без загрузки бинарников)";
});

commentForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const text = commentTextInput.value.trim();
    if (!text) {
        return;
    }

    const attachments = Array.from(commentFilesInput.files || []).map((f) => f.name);
    try {
        await apiRequest(`/tickets/${encodeURIComponent(currentTicketId)}/comments`, {
            method: "POST",
            body: JSON.stringify({ text, attachments })
        });
        commentForm.reset();
        selectedFilesNote.textContent = "Имена выбранных файлов передаются строкой (без загрузки бинарников)";
        showBanner("Комментарий добавлен.");
        await loadTicket();
    } catch (error) {
        alert(error.message);
    }
});

assignForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const formData = new FormData(assignForm);
    const assigneeId = formData.get("assigneeId")?.toString() || null;
    const reason = formData.get("reason")?.toString().trim() || null;

    try {
        const result = await apiRequest(`/tickets/${encodeURIComponent(currentTicketId)}/assign`, {
            method: "PATCH",
            body: JSON.stringify({ assigneeId, reason })
        });
        assignForm.reset();
        assignDialog.close();
        showBanner(result?.message || "");
        await loadTicket();
    } catch (error) {
        alert(error.message);
    }
});

reviewForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const formData = new FormData(reviewForm);
    const payload = {
        resolution: formData.get("resolution")?.toString(),
        releaseVersion: null,
        mergeRequestUrl: formData.get("mergeRequestUrl")?.toString().trim() || null,
        originalTicketId: null,
        justification: formData.get("justification")?.toString().trim() || null
    };

    try {
        const result = await apiRequest(`/tickets/${encodeURIComponent(currentTicketId)}/review`, {
            method: "POST",
            body: JSON.stringify(payload)
        });
        reviewForm.reset();
        reviewDialog.close();
        showBanner(result?.message || "");
        await loadTicket();
    } catch (error) {
        alert(error.message);
    }
});

async function init() {
    try {
        await Promise.all([loadCurrentUser(), loadTicket()]);
    } catch (error) {
        showBanner(`Не удалось загрузить тикет: ${error.message}`);
    }
}

init();
