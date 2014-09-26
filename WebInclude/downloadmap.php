<?php 
//include me where you want your accept code to run.
include_once "mapfuncs.php";
//done until additional map metadata is added.

$cmrID = getCMRID();
$mapname = urldecode($_GET['map']);
$mappath = 'C:\\CMR\\Maps\\' . $cmrID . '\\pending\\' . $mapname;
if (file_exists($mappath)) {
	header("X-Sendfile: $mappath");
	header("Content-type: application/octet-stream");
	header('Content-Disposition: attachment; filename="' . basename($mappath) . '"');
} else {
	$_SESSION['warning'] = "error, that file doesnt exist. pls.";
header('Location: http://eklipz.us.to/cmr/maptest.php');
}
?>