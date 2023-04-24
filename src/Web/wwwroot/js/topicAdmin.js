var confirmDeletePostModal = document.getElementById("confirmDeletePostModal");

function openDeletePostModal(id, title) {
    document.getElementById("deletePostId").value = id;
    document.getElementById("deletePostTitle").innerText = title;
    confirmDeletePostModal.style.display = 'block';
}

function closeDeletePostModal() {
    confirmDeletePostModal.style.display = 'none';
}

function submitDeletePost() {
    document.getElementById("deletePostForm").submit();
}

window.onclick = function(event) {
    if (event.target == confirmDeletePostModal) {
        confirmDeletePostModal.style.display = 'none';
    }
}