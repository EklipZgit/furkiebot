<?php
//$DEBUG = false;

//ob_start();
session_start();
//		if ($DEBUG) { echo "starting loginsubmit.php"; }

$username = strtolower($_POST['username']);
$password = $_POST['password'];

$userlistfile = "C:\\CMR\\Data\\Userlist\\userlistmap.json";
$filestring = file_get_contents($userlistfile);
$userarray = json_decode($filestring, true);

if (array_key_exists($username, $userarray)) {
	//		if ($DEBUG) {echo "user in array<br>";}
	$userData = $userarray[$username];
	$usernameCase = $userData['ircname'];
	$salt = $userData['salt'];
	$pwhash = base64_encode(hash('sha256', $password, true));
	$hash =  base64_encode(hash('sha256', $userData['salt'] . $pwhash , true));
	$expected = $userData['password'];

	if($hash != $userData['password']) // Incorrect password. So, redirect to login_form again.
	{	
		//		if ($DEBUG) { echo "bad password<br>";	}
		$_SESSION['warning'] = "BAD PASSWORD";
		header('Location: login.php');
	} else { // Logged in successfully.
		session_regenerate_id();
		$_SESSION['sess_user_id'] = $username;
		$_SESSION['username'] = $username;
		$_SESSION['usernameCase'] = $usernameCase;
		$_SESSION['loggedIn'] = true;
		$_SESSION['trusted'] = $userData['trusted'];
		$_SESSION['admin'] = $userData['admin'];
		$_SESSION['tester'] = $userData['tester'];
		$_SESSION['LAST_ACTIVITY'] = time(); 
		//		if ($DEBUG) {echo "logged in successfully<br>";}
		if (isset($_SESSION['redirect'])) {
			$temp = $_SESSION['redirect'];
			unset($_SESSION['redirect']);
			//		if ($DEBUG) {echo "redirecting to " . $temp . "<br>";}
			session_write_close();
			header('Location: ' . $temp);
		} else {
			session_write_close();
			//		if ($DEBUG) {echo "redirecting to index<br>";}
			header('Location: index.php');
		}
	}
} else {
	//		if ($DEBUG) {echo "user not in array, redirecting to login<br>";}
	$_SESSION['warning'] = 'NOT A REGISTERED USERNAME, REGISTRATION INFO <a href="register.php">HERE</a>';
	session_write_close();
	header('Location: login.php');
}
?>