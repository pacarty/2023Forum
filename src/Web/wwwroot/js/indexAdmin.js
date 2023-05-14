var confirmDeleteCategoryModal = document.getElementById("confirmDeleteCategoryModal");
var confirmDeleteTopicModal = document.getElementById("confirmDeleteTopicModal");

function openDeleteCategoryModal(id, name) {
    document.getElementById("deleteCategoryId").value = id;
    document.getElementById("deleteCategoryName").innerText = name;
    confirmDeleteCategoryModal.style.display = 'block';
}

function closeDeleteCategoryModal() {
    confirmDeleteCategoryModal.style.display = 'none';
}

function openDeleteTopicModal(id, name) {
    document.getElementById("deleteTopicId").value = id;
    document.getElementById("deleteTopicName").innerText = name;
    confirmDeleteTopicModal.style.display = 'block';
}

function closeDeleteTopicModal() {
    confirmDeleteTopicModal.style.display = 'none';
}

function submitDeleteCategory() {
    document.getElementById("deleteCategoryForm").submit();
}

function submitDeleteTopic() {
    document.getElementById("deleteTopicForm").submit();
}

// When a user clicks outside the modal window, it exits the modal
window.onclick = function(event) {
    if (event.target == confirmDeleteCategoryModal) {
        confirmDeleteCategoryModal.style.display = 'none';
    }
    if (event.target == confirmDeleteTopicModal) {
        confirmDeleteTopicModal.style.display = 'none';
    }
}