<?php 
	include_once "funcs.php";
	$cmrID = getCMRID();
	$maps = getMaps();
?>
<table id="pending">
<thead><h4>Maps that need testing: </h4></thead>
	<?php 
	foreach ($maps as $key => $value) {
		$pendingcount = 0;
		if (!$value['accepted']) {
			$pendingcount++;
			echo '<tr class="pendingrow">';
			echo '<td class="pendingname">' . $value['author'] . '</td>';
			echo '<td class="pendingmap">' . $value['name'] . '</td>';
			echo '<td class="pendingdownload"><a href="downloadmap.php?map=' . urlencode($value['name']) . '">Download</a></td>';
			echo '<td class="pendinglink"><a href="accept.php?map=' . urlencode($value['name']) . '">Accept this map</a></td>';
			echo '<td class="pendinglink"><a href="deny.php?map=' . urlencode($value['name']) . '">Deny this map</a></td>';
			echo '</tr>';
		}
	}
	?>
</table>
<div style="height:50px"></div>

<table id="accepted">
<thead><h4>Already accepted maps: </h4></thead>
	<?php 
	foreach ($maps as $key => $value) {
		$acceptedcount = 0;
		if ($value['accepted']) {
			$acceptedcount++;
			echo '<tr class="pendingrow">';
			echo '<td class="pendingname">' . $value['author'] . '</td>';
			echo '<td class="pendingmap">' . $value['name'] . '</td>';
			echo '<td class="pendingmap">' . $value['acceptedBy'] . '</td>';
			echo '<td class="pendingdownload"><a href="downloadmap.php?map=' . urlencode($value['name']) . '">Download</a></td>';
			echo '<td class="pendinglink"><a href="unaccept.php?map=' . urlencode($value['name']) . '">Unaccept this map</a></td>';
			echo '</tr>';
		}
	}
	?>
</table>