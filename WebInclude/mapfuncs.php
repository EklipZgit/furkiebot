<?php 	

	function getCMRID() {
		if (isset($cmrID)) {
			return $cmrID;
		} else if (file_exists('C:/CMR/Data/CMR_ID.txt')) {
			return intval(trim(file_get_contents('C:/CMR/Data/CMR_ID.txt')));
		} else { //file doesnt exist.
			throw new Exception('Nigga pls you dont even have a C:/CMR/Data/CMR_ID.txt file with a number in it.');
			return -1;
		}
	} 
?>