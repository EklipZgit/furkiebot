<!DOCTYPE html>
<?php include "../WebInclude/navbar.php"; // YOU NEED TO INCLUDE THIS FILE AT THE TOP OF THE PAGE.
?> 

<html lang="en">
  <head>
    <title>Login</title>

    <!-- Bootstrap core CSS -->
    <link href="css/bootstrap.min.css" rel="stylesheet">

    <!-- Custom styles for this template -->
    <link href="css/login.css" rel="stylesheet">
  </head>

  <body>

	<div class="skinny">

<?php displayNavbar("Log in!");?>
			<center><h3 class="heading">Log in with your FurkieBot registration details!</h3></center>
				<form class="form-signin" id="form1" name="form1" role="form" method="post" action="loginsubmit.php">
				<input type="username" class="form-control" placeholder="Username" name="username" id="username" required autofocus>
				<input type="password" class="form-control" placeholder="Password" name="password" id="password"  required>
				<button class="btn btn-lg btn-primary btn-block" type="submit" value="Submit" >Log in</button>
				</form>

					<center>
  					<?php
  					ensureSession();
  					include "../WebInclude/displaymessages.php";
  					session_write_close();
  					?>
  					</center>
  					
			<p><b>Haven't registered with FurkieBot yet? <a href="register.php">Learn how to register</a></b></p>
			<p>Forgot your CMR password? Just follow the registration instructions above to register a new password. <br>Don't worry, this won't reset your race history or anything like that.</br></p>
			<p>Forgot your SpeedRunsLive identify password? You will need to follow their instructions for resetting below:</p>
				
			<div class="well">
				If you forgot your SRL nickserv password, you can request an e-mail with instructions to reset it using <br>"/nickserv resetpass (nickname)". The e-mail may be filtered by your spam filter.
			</div>
			<p>Switched SRL nicks and need your info transferred onto a new nick? Talk to a CMR Admin. We won't be happy with you, but for now it is doable.</p>
				
    </div>
  </body>
</html>