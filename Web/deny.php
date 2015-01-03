<?php 
	session_start();
	include_once "../WebInclude/funcs.php";
	if (isLoggedIn()) {
		$maps = getMaps();
		$mapper = $maps[$_GET['map']]->author;
		$_SESSION['warning'] = "For now, just join IRC and tell " . $mapper . " that his map has been denied, and why. Let him know he should fix the issues and reupload it with the same name.";
		session_write_close();
		header("Location: /maptest.php");
	} else {
		$_SESSION['redirect'] = "/maptest.php";
		$_SESSION['warning'] = "You need to log in before testing maps.";
		session_write_close();
		header( 'Location: /login.php' );
	}
?>