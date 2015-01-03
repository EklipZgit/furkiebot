<?php 
	//include me where you want your accept code to run.
	include_once "../WebInclude/funcs.php";
	//done until additional map metadata is added.
	
	session_start();
	if (isLoggedIn()) {
		if (isTester() || isAdmin()) {
			$cmrID = getCMRID();
			$maps = getMaps();
			$mapname = urldecode($_GET['map']);
			$nameLower = strtolower($mapname);
			$mappath = 'C:\\CMR\\Maps\\' . $cmrID . '\\' . $mapname;

			if (array_key_exists($nameLower, $maps)) {
				$maps[$nameLower]->accepted = false;
				$maps[$nameLower]->acceptedBy = "";
				writeMaps($maps);
				
				$_SESSION['message'] = "You successfully un-accepted " . $mapname;
			} else {
				$_SESSION['warning'] = "error, you tried to un-accept a file that doesnt exist.";
			}
			session_write_close();
			header('Location: maptest.php');
		} else {
			include "../WebInclude/testeroption.php";
		}
	} else {
		$_SESSION['redirect'] = "maptest.php";
		$_SESSION['warning'] = "You need to log in before testing maps.";
		session_write_close();
		header( 'Location: login.php' );
	}
?>