<?php 
	$mapper = explode("-", urldecode($_GET['map']), 2)[0];
	$_SESSION['warning'] = "For now, just join IRC and tell " . $mapper . " that his map has been denied, and why. Let him know he should fix the issues and reupload it with the same name.";
	header("../Web/maptest.php");
?>