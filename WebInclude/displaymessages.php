<?php 
	if (isset($_SESSION['warning'])) {
		echo '<p style="color:red;"><b>' . $_SESSION['warning'] . '</b></p>';
		unset($_SESSION['warning']);
	}
	if (isset($_SESSION['message'])) {
		echo '<p style="color:green;"><b>' . $_SESSION['message'] . '</b></p>';
		unset($_SESSION['message']);
	}
?>