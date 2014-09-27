<?php 	
	function isLoggedIn() {
		if (isset($_SESSION['loggedIn']) && $_SESSION['loggedIn'] == true) {
			if (isset($_SESSION['LAST_ACTIVITY']) && (time() - $_SESSION['LAST_ACTIVITY'] > 1800)) {
			    // last request was more than 30 minutes ago
			    session_unset();     // unset $_SESSION variable for the run-time 
			    session_destroy();   // destroy session data in storage
			    return false;
			} else if (!isset($_SESSION['LAST_ACTIVITY'])) {
				$_SESSION['loggedIn'] = false;
				return false;
			} else {

				$_SESSION['LAST_ACTIVITY'] = time(); 
				return true;
			}
		} else return false;
	}
	
	function ensureSession() {
		if (session_status() == PHP_SESSION_NONE) {
		    session_start();
		}
	}

	
	function isTester() {
	
	}
	
	function isTrusted() {
	
	}


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