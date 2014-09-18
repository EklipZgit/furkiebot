<?php
require_once "curl.php";
ob_start();
session_start();
 echo "<html><head></head><body>";
$username = $_POST['username'];
$password = $_POST['password'];

$userlistfile = "C:\CMR\Data\Userlist\userlistmap.json";
$filestring = file_get_contents($userlistfile);
echo "user: " . $username . "<br>";
$userarray = json_decode($filestring, true);

$userData = $userarray[$username];
$salt = $userData['salt'];
$pwhash = base64_encode(hash('sha256', $password, true));
$hash =  base64_encode(hash('sha256', $userData['salt'] . $pwhash , true));
$expected = $userData['password'];
 
if($hash != $userData['password']) // Incorrect password. So, redirect to login_form again.
{
	echo "BAD PASSWORD";
   // header('Location: login.html');
} else { // Redirect to home page after successful login.
	session_regenerate_id();
	$_SESSION['sess_user_id'] = $userData['username'];
	$_SESSION['sess_username'] = $userData['username'];
	session_write_close();
	echo "GOOD PASSWORD";
	//header('Location: home.php');
}
echo "</body></html>";
?>