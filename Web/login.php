<head>
<title>Login Form</title>
<style>
#main {
	width: 800px;
	text-alight: left;
	align: center;
}
</style>
</head>
<body>
<?php
start_session();
if (isset($_SESSION['warning'])) {
	echo '<p style="color:red;"><b>' . $_SESSION['warning'] . '</b></p>';
	unset($_SESSION['warning']);
}
session_write_close();
?>
<div id="main">
<form id="form1" name="form1" method="post" action="loginsubmit.php">
	<table width="510" border="0" align="center">
		<tr>
			<td colspan="2">Login Form</td>
		</tr>
		<tr>
			<td>Username:</td>
			<td><input type="text" name="username" id="username" /></td>
		</tr>
		<tr>
			<td>Password</td>
			<td><input type="password" name="password" id="password" /></td>
		</tr>
		<tr>
			<td>&nbsp;</td>
			<td><input type="submit" name="button" id="button" value="Submit" /></td>
		</tr>
	</table>
</form>

<p>
Havent registered with FurkieBot yet? You need to register first before you can login here.<br>Go <a href="http://eklipz.us.to/cmr/register.html">HERE</a> to learn how to register. It's easy!
</p>
<p>Forgot your CMR password? Just follow the registration instructions above to register a new password. Don't worry, this wont reset your race history or anything like that.
</p>

<p>Forgot your SpeedrunsLive Ident password? You will need to follow their instructions for resetting below:
</p>
If you forgot your password, you can request an e-mail with instructions to reset it using "/nickserv resetpass (nickname)". The e-mail may be filtered by your spam filter.

<p>Switched SRL nicks and need your info transferred onto a new nick? Talk to an admin. We wont be happy with you, but for now it is doable.</p>
</div>
</body>
</html>