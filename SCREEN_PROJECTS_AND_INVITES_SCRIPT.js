const invitations = document.querySelector(".invitations");
const title = invitations.querySelector("h2");

function updateInvitations() {
    const count = invitations.querySelectorAll(".invite").length;

    title.textContent = `Входящие приглашения (${count})`;
    invitations.hidden = count === 0;
}

for (const invite of invitations.querySelectorAll(".invite")) {
    const decline = invite.querySelector(".decline");
    const accept = invite.querySelector(".actions a");

    function removeInvite() {
        invite.remove();
        updateInvitations();
    }

    decline.addEventListener("click", removeInvite);
    accept.addEventListener("click", removeInvite);
}

updateInvitations();