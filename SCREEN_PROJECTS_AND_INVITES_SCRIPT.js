const invitationsSection = document.getElementById("invitations-section");
const invitationsTitle = document.getElementById("invitations-title");
const invitationsList = document.getElementById("invitations-list");
const projectsList = document.getElementById("projects-list");

const createDialog = document.getElementById("create-project-dialog");
const createForm = document.getElementById("create-project-form");
const createError = document.getElementById("create-project-error");

bindDialog(
    createDialog,
    document.getElementById("open-create-project"),
    [
        document.getElementById("close-create-project"),
        document.getElementById("cancel-create-project")
    ]
);

function renderInvites(invites) {
    invitationsTitle.textContent = `Входящие приглашения (${invites.length})`;
    invitationsSection.hidden = invites.length === 0;

    if (invites.length === 0) {
        invitationsList.innerHTML = "";
        return;
    }

    invitationsList.innerHTML = invites.map((invite) => {
        const roles = (invite.offeredRoles || [])
            .map((role) => ROLE_LABELS[role] || role)
            .join(", ");

        return `
            <article class="invite" data-invite-id="${escapeHtml(invite.id)}">
                <div class="invitation-info">
                    <header class="invitation-header">
                        <h3>${escapeHtml(invite.projectName)}</h3>
                        <span class="role">Роли: ${escapeHtml(roles)}</span>
                    </header>
                    <footer class="invite-footer">
                        <p>Приглашение от ${escapeHtml(invite.inviterName)}.</p>
                        <small>Действует до: ${escapeHtml(formatDate(invite.expiresAt))}</small>
                    </footer>
                </div>
                <div class="actions">
                    <button class="button decline" type="button" data-action="decline">Отклонить</button>
                    <button class="button" type="button" data-action="accept">Принять</button>
                </div>
            </article>
        `;
    }).join("");
}

function renderProjects(projects) {
    if (projects.length === 0) {
        projectsList.innerHTML = '<p class="response-status">У вас пока нет активных проектов.</p>';
        return;
    }

    projectsList.innerHTML = projects.map((project) => {
        const rolesHtml = (project.currentUserRoles || [])
            .map((role) => `<span class="role">${escapeHtml(ROLE_LABELS[role] || role)}</span>`)
            .join("");

        return `
            <article class="project-card">
                <header class="project-header">
                    <h3>${escapeHtml(project.name)}</h3>
                    <div class="project-roles">${rolesHtml}</div>
                </header>
                <p>${escapeHtml(project.description || "Без описания")}</p>
                <p class="repo-info">Git Remote: ${escapeHtml(project.gitRemoteUrl)} | Релиз: ${escapeHtml(project.softwareVersion)}</p>
                <footer class="project-footer">
                    <small>Открытых тикетов: ${escapeHtml(project.openTicketsCount)}</small>
                    <a href="SCREEN_TICKET_REGISTRY.html?projectId=${encodeURIComponent(project.id)}">Перейти к тикетам →</a>
                </footer>
            </article>
        `;
    }).join("");
}

async function refreshDashboard() {
    const [invites, projects] = await Promise.all([
        apiRequest("/invites/my"),
        apiRequest("/projects")
    ]);
    renderInvites(invites || []);
    renderProjects(projects || []);
}

invitationsList.addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) {
        return;
    }

    const card = button.closest("[data-invite-id]");
    const inviteId = card?.dataset.inviteId;
    if (!inviteId) {
        return;
    }

    button.disabled = true;
    try {
        const action = button.dataset.action === "accept" ? "accept" : "decline";
        await apiRequest(`/invites/${encodeURIComponent(inviteId)}/${action}`, { method: "POST" });
        await refreshDashboard();
    } catch (error) {
        alert(error.message);
        button.disabled = false;
    }
});

createForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    createError.hidden = true;

    const formData = new FormData(createForm);
    const roles = formData.getAll("roles");
    const payload = {
        name: formData.get("name")?.toString().trim(),
        description: formData.get("description")?.toString().trim() || null,
        softwareVersion: formData.get("softwareVersion")?.toString().trim(),
        gitRemoteUrl: formData.get("gitRemoteUrl")?.toString().trim(),
        creatorRoles: roles.length > 0 ? roles : ["Leader"]
    };

    try {
        await apiRequest("/projects", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        createForm.reset();
        createDialog.close();
        await refreshDashboard();
    } catch (error) {
        createError.textContent = error.message;
        createError.hidden = false;
    }
});

async function init() {
    try {
        await Promise.all([loadCurrentUser(), refreshDashboard()]);
    } catch (error) {
        projectsList.innerHTML = `<p class="response-status">Не удалось загрузить данные: ${escapeHtml(error.message)}</p>`;
    }
}

init();
