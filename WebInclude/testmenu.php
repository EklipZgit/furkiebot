<?php 
	include_once "funcs.php";
	$cmrID = getCMRID();
	$pending = scandir("C:\\CMR\\Maps\\" . $cmrID . "\\pending");

	$accepted = scandir("C:\\CMR\\Maps\\" . $cmrID . "\\accepted");

?>
<table id="pending">
<thead><h4>Maps that need testing: <?php echo count($pending) - 2; ?></h4></thead>
	<?php 
	for ($i = 0; $i < count($pending); $i++) {
		if ($pending[$i] != "." && $pending[$i] != "..") {
			$split = explode("-", $pending[$i], 2);
			$mapname = $split[1];
			$mapper = $split[0];
			echo '<tr class="pendingrow">';
			echo '<td class="pendingmap">' . $mapname . '</td>';
			echo '<td class="pendingname">' . $mapper . '</td>';
			echo '<td class="pendingdownload"><a href="downloadmap.php?map=' . urlencode($pending[$i]) . '">Download</a></td>';
			echo '<td class="pendinglink"><a href="accept.php?map=' . urlencode($pending[$i]) . '">Accept this map</a></td>';
			echo '<td class="pendinglink"><a href="deny.php?map=' . urlencode($pending[$i]) . '">Deny this map</a></td>';
			echo '</tr>';
		}
	}
	?>
</table>
<div style="height:50px"></div>

<table id="accepted">
<thead><h4>Already accepted maps: <?php echo count($accepted) - 2; ?></h4></thead>
	<?php 
	for ($i = 0; $i < count($accepted); $i++) {
		if ($accepted[$i] != "." && $accepted[$i] != "..") {
			$split = explode("-", $accepted[$i], 2);
			$mapname = $split[1];
			$mapper = $split[0];
			echo '<tr class="pendingrow">';
			echo '<td class="pendingmap">' . $mapname . '</td>';
			echo '<td class="pendingname">' . $mapper . '</td>';
			echo '<td class="pendingdownload"><a href="downloadmap.php?map=' . urlencode($accepted[$i]) . '">Download</a></td>';
			echo '<td class="pendinglink"><a href="unaccept.php?map=' . urlencode($accepted[$i]) . '">Unaccept this map</a></td>';
			echo '</tr>';
		}
	}
	?>
</table>