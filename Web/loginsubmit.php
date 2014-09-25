<?php
require_once "curl.php";
//ob_start();
session_start();

$username = strtolower($_POST['username']);
$password = $_POST['password'];

$userlistfile = "C:\\CMR\\Data\\Userlist\\userlistmap.json";
$filestring = file_get_contents($userlistfile);
$userarray = json_decode($filestring, true);

if (array_key_exists($username, $userarray)) {
	echo "key existed, user in array";
	$userData = $userarray[$username];
	$usernameCase = $userData['ircname'];
	$salt = $userData['salt'];
	$pwhash = base64_encode(hash('sha256', $password, true));
	$hash =  base64_encode(hash('sha256', $userData['salt'] . $pwhash , true));
	$expected = $userData['password'];
	 
	if($hash != $userData['password']) // Incorrect password. So, redirect to login_form again.
	{
		$_SESSION['warning'] = "BAD PASSWORD";
		header('Location: login.php');
	} else { // Logged in successfully.
		session_regenerate_id();
		$_SESSION['sess_user_id'] = $username;
		$_SESSION['username'] = $usernameCase;
		$_SESSION['loggedIn'] = true;
		$_SESSION['trusted'] = $userData['trusted'];
		$_SESSION['admin'] = $userData['admin'];
		$_SESSION['tester'] = $userData['tester'];
		
		if (isset($_SESSION['redirect'])) {
			$temp = $_SESSION['redirect'];
			unset($_SESSION['redirect']);
			session_write_close();
			header('Location: ' + $temp);
		} else {
			session_write_close();
			header('Location: http://eklipz.us.to/cmr');
		}
	}
} else {
	$_SESSION['warning'] = 'NOT A REGISTERED USERNAME, REGISTRATION INFO <a href="http://eklipz.us.to/cmr/register.html">HERE</a>';
	session_write_close();
	header('Location: login.php');
}
?>