<?php
$temp = explode(".", $_FILES["file"]["name"]);
$extension = end($temp);

if (count($temp) == 1) { //no file extensions.
	echo "Thanks " . $_POST["user"] . "!<br>";
	if ($_FILES["file"]["error"] > 0) {
	  echo "ERROR UPLOADING. Return Code: " . $_FILES["file"]["error"] . "<br>";
	  echo "screenshot this to EklipZ in #DFcmr";
	} else {
	  if (file_exists("C:/CMR/maps/36/pending/" . $_POST["user"] . "-" . $_POST["mapname"])) {
		unlink("C:/CMR/Maps/36/pending/" . $_POST["user"] . "-" . $_POST["mapname"]);
		move_uploaded_file($_FILES["file"]["tmp_name"],
		"C:/CMR/Maps/36/pending/" . $_POST["user"] . "-" . $_POST["mapname"]);
	  echo "Replaced: " . $_POST["user"] . "-" . $_POST["mapname"] . " successfully.<br>";
	  } else {
		move_uploaded_file($_FILES["file"]["tmp_name"],
		"C:/CMR/Maps/36/pending/" . $_POST["user"] . "-" . $_POST["mapname"]);
	  echo "Uploaded: " . $_POST["user"] . "-" . $_POST["mapname"] . " successfully.<br>";
	  }
	}
} else if ($extension == "html" || $extension == "php"){
	echo "wow ok fuck you too :>";
} else {
	echo "uh oh you had a \".\" in the filename. Please, no files with extensions or \".\"'s";
	echo "<br><a href=\"map.html\">Resubmit</a>";
}
?>