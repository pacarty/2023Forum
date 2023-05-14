// Registering a user
function submitRegister() {
	var userField = document.getElementById("registerUsername").value;
	var passwordField = document.getElementById("registerPassword").value;
	var confirmPasswordField = document.getElementById("registerConfirmPassword").value;

	var validationElement = document.getElementById("registerValidation");

	if (userField.trim() == "" || passwordField.trim() == "" || confirmPasswordField.trim() == "") {
		validationElement.innerText = "Fields cannot be empty";
		return;
	}

	if (passwordField != confirmPasswordField) {
		validationElement.innerText = "Passwords do not match";
		return;
	}

	$.ajax({
		type: "GET",
		url: "/Account/TestIfUserExists",
		data: { username: userField },
		dataType: "text",
		success: function (isUsernameTaken) {
			if (isUsernameTaken == "true") {
				validationElement.innerText = "username already taken";
				return;
			} else {
				document.getElementById("registerForm").submit();
				return;
			}
		},
		error: function (req, status, error) {
			console.log(error);
		}
	});
}

// Authenticating user
function submitLogin() {
	var userField = document.getElementById("loginUsername").value;
	var passwordField = document.getElementById("loginPassword").value;
	var validationElement = document.getElementById("loginValidation");

	if (userField.trim() == "" || passwordField.trim() == "") {
		validationElement.innerText = "Fields cannot be empty";
		return;
	}

	$.ajax({
		type: "GET",
		url: "/Account/VerifyUser",
		data: {
			username: userField,
			password: passwordField
		},
		dataType: "text",
		success: function (isUserAuthorized) {
			if (isUserAuthorized == "" || isUserAuthorized == null) {
				validationElement.innerText = "username or password is incorrect";
				return;
			} else {
				document.getElementById("loginForm").submit();
				return;
			}
		},
		error: function (req, status, error) {
			console.log(error);
		}
	});
}

function submitLogout() {
	document.getElementById("logoutForm").submit();
}

function submitAddCategory() {
	var categoryField = document.getElementById("categoryName").value;
	var validationElement = document.getElementById("addCategoryValidation");

	if (categoryField.trim() == "") {
		validationElement.innerText = "Cannot be empty";
		return;
	}

	document.getElementById("addCategoryForm").submit();
}

function checkAddCategory() {

	var categoryField = document.getElementById("categoryName").value;
	var validationElement = document.getElementById("addCategoryValidation");

	if (categoryField.trim() != "") {
		validationElement.innerText = "";
	}
}

function submitAddTopic() {
	var topicField = document.getElementById("topicName").value;
	var validationElement = document.getElementById("addTopicValidation");

	if (topicField.trim() == "") {
		validationElement.innerText = "Cannot be empty";
		return;
	}

	document.getElementById("addTopicForm").submit();
}

function checkAddTopic() {

	var topicField = document.getElementById("topicName").value;
	var validationElement = document.getElementById("addTopicValidation");

	if (topicField.trim() != "") {
		validationElement.innerText = "";
	}
}

function submitAddPost() {
	var contentField = document.getElementById("commentContent").value;
	var postField = document.getElementById("postTitle").value;
	var validationElement = document.getElementById("addPostValidation");

	if (contentField.trim() == "" || postField.trim() == "") {
		validationElement.innerText = "Fields cannot be empty";
		return;
	}

	document.getElementById("addPostForm").submit();
}

function checkAddPost() {
	var contentField = document.getElementById("commentContent").value;
	var postField = document.getElementById("postTitle").value;
	var validationElement = document.getElementById("addPostValidation");

	if (contentField.trim() != "" && postField.trim() != "") {
		validationElement.innerText = "";
	}
}

function submitAddComment() {
	var contentField = document.getElementById("content").value;
	var validationElement = document.getElementById("addCommentValidation");

	if (contentField.trim() == "") {
		validationElement.innerText = "Cannot be empty";
		return;
	}

	document.getElementById("addCommentForm").submit();
}

function checkAddComment() {

	var contentField = document.getElementById("content").value;
	var validationElement = document.getElementById("addCommentValidation");

	if (contentField.trim() != "") {
		validationElement.innerText = "";
	}
}

function submitEditUserRole() {
	document.getElementById("editUserRoleForm").submit();
}

function submitEditUserBanned() {
	document.getElementById("editUserBannedForm").submit();
}

function submitEditShowModControls() {
	document.getElementById("editShowModControlsForm").submit();
}

function checkIfModSelected(role) {
	if (role == "Moderator") {
		document.getElementById("editModerationDIV").style.display = 'block';
	} else {
		document.getElementById("editModerationDIV").style.display = 'none';
	}
}

function editComment(id) {
	document.getElementById("editDeleteDIV_" + id).style.display = 'none';
	document.getElementById("comment_show_" + id).style.display = 'none';
    document.getElementById("comment_edit_" + id).style.display = 'block';
}

function cancelEdit(id) {
	document.getElementById("comment_edit_" + id).style.display = 'none';
	document.getElementById("comment_show_" + id).style.display = 'block';
	document.getElementById("editDeleteDIV_" + id).style.display = 'block';
}

function deleteComment(id) {
	document.getElementById("editDeleteDIV_" + id).style.display = 'none';
    document.getElementById("confirmDeleteDIV_" + id).style.display = 'block';
}

function cancelDeleteComment(id) {
	document.getElementById("confirmDeleteDIV_" + id).style.display = 'none';
    document.getElementById("editDeleteDIV_" + id).style.display = 'block';
}

function submitEditComment(id) {
	var contentField = document.getElementById("editCommentContent").value;
	var validationElement = document.getElementById("editCommentValidation");

	if (contentField.trim() == "") {
		validationElement.innerText = "Cannot be empty";
		return;
	}

	document.getElementById("editCommentForm_" + id).submit();
}

function checkEditComment() {

	var contentField = document.getElementById("editCommentContent").value;
	var validationElement = document.getElementById("editCommentValidation");

	if (contentField.trim() != "") {
		validationElement.innerText = "";
	}
}

function submitDeleteComment(id) {
	document.getElementById("deleteCommentForm_" + id).submit();
}