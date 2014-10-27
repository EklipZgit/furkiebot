<?php 
include_once "../WebInclude/funcs.php";
//done until additional map metadata is added.
ensureSession();
$mapID = trim(urldecode($_GET['id']));
$mapName = trim(urldecode($_GET['name']));
header("Location: dustforce://install/" . $mapID . "/" . $mapName);
// header("X-Sendfile: $mappath");
// header("Content-type: application/octet-stream");
// header('Content-Disposition: attachment; filename="' . basename($mappath) . '"');
exit;

?>