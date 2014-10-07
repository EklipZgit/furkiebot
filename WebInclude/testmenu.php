<?php
	include_once "funcs.php";
	$cmrID = getCMRID();
	$maps = getMaps();
?>
<table id="pending">
<thead><h4>Maps that need testing: </h4></thead>
	<?php 
	foreach ($maps as $key => $map) {
		$pendingcount = 0;
		if (!$map->accepted) {
			$pendingcount++;
			echo '<tr class="pendingrow">';
			echo '<td class="pendingname">' . $map->author . '</td>';
			echo '<td class="pendingmap">' . $map->name . '</td>';
			echo '<td class="pendingdownload"><a href="downloadmap.php?map=' . urlencode($map->name) . '">Download</a></td>';
			echo '<td class="pendinglink"><a href="accept.php?map=' . urlencode($map->name) . '">Accept</a></td>';
			echo '<td class="pendinglink"><a href="deny.php?map=' . urlencode($map->name) . '">Deny</a></td>';
			echo '<td class="pendinglink"><a href="deletemap.php?map=' . urlencode($map->name) . '">Delete</a></td>';
			echo '</tr>';
		}
	}
	?>
</table>
<div style="height:50px"></div>

<table id="accepted">
<thead><h4>Already accepted maps: </h4></thead>
	<?php 
	foreach ($maps as $nameLower => $map) {
		$acceptedcount = 0;
		if ($map->accepted) {
			$acceptedcount++;
			echo '<tr class="pendingrow">';
			echo '<td class="pendingname">' . $map->author . '</td>';
			echo '<td class="pendingmap">' . $map->name . '</td>';
			echo '<td class="pendingmap">' . $map->acceptedBy . '</td>';
			echo '<td class="pendingdownload"><a href="downloadmap.php?map=' . urlencode($map->name) . '">Download</a></td>';
			echo '<td class="pendinglink"><a href="unaccept.php?map=' . urlencode($map->name) . '">Unaccept</a></td>';
			echo '<td class="pendinglink"><a href="deletemap.php?map=' . urlencode($map->name) . '">Delete</a></td>';
			echo '</tr>';
		}
	}
	?>
</table>