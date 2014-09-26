<?php 
	include_once "funcs.php";
	$cmrID = getCMRID();
	$maps = scandir("C:\\CMR\\Maps\\" . $cmrID . "\\pending");

?>
<table id="tester">
	<?php 
	for ($i = 0; $i < count($maps); $i++) {
		if ($maps[$i] != "." && $maps[$i] != "..") {
			echo '<tr class="maprow">';
			echo '<td class="mapname">' . $maps[$i] . '</td>';
			echo '<td class="maplink"><a href="downloadmap.php?map=' . urlencode($maps[$i]) . '">Download</a></td>';
			echo '<td class="maplink"><a href="accept.php?map=' . urlencode($maps[$i]) . '">Accept this map</a></td>';
			echo '<td class="maplink"><a href="deny.php?map=' . urlencode($maps[$i]) . '">Deny this map</a></td>';
			echo '</tr>';
		}
	}
?>
</table>