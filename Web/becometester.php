<?php
include_once "../WebInclude/funcs.php";
//becometester.php
session_start();
if (isTrusted()) {
	if (!isTester()) {
		$userlist = getUserList();
		$userlist[$_SESSION['username']]['tester'] = true;
		writeUserList($userlist);
		$_SESSION['message'] = "You have now become a tester for the upcoming CMR. Don't shirk your responsibilities!";		
	} else {
		$_SESSION['warning'] = "you're already a tester???";
	}
	session_write_close();
	header("Location: maptest.php");
} 
?>