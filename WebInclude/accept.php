<?php 
	//include me where you want your accept code to run.
	include_once "mapfuncs.php";
	//done until additional map metadata is added.
	
	$cmrID = getCMRID();
	$mapname = urldecode($_GET['map']);
	$mappath = 'C:\\CMR\\Maps\\' . $cmrID . '\\pending\\' . $mapname;
	$targetpath = 'C:\\CMR\\Maps\\' . $cmrID . '\\accepted\\' . $mapname;

	if (file_exists($mappath)) {
		rename($mappath, $targetpath);
		$_SESSION['message'] = "You successfully accepted " . $mapname;
	} else {
		$_SESSION['warning'] = "error, you tried to accept a file that doesnt exist.";
	}
	header('Location: http://eklipz.us.to/cmr/maptest.php');
?>