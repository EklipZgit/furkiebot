<?php
include_once "funcs.php";
if (isTester()) {
	echo "<p>You are already a tester. Once set as a tester, only an admin can make you not a tester for this CMR, to prevent cheating.</p>";
} else if (isTrusted()) {
	echo '<p>If you wish to become a tester, click <a href="becometester.php">HERE</a>. Warning: Once you become a tester, you cannot participate in the upcoming CMR. This is non-reversable. You will no longer be a tester after this CMR.</p>';
} else {
	echo '<p>You need to be a trusted community member to be allowed to set yourself as a tester. Ask an admin for testing permissions in IRC.</p>';
}

?>