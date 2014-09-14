<?php
$temp = explode(".", $_FILES["file"]["name"]);
echo "Thanks " . $_POST["user"] . "!<br>";

if ($_FILES["file"]["error"] > 0) {
  echo "ERROR UPLOADING. Return Code: " . $_FILES["file"]["error"] . "<br>";
  echo "screenshot this to EklipZ in #DFcmr";
} else {
  if (file_exists("../pending/" . $_POST["user"] . "-" . $_POST["mapname"])) {
    move_uploaded_file($_FILES["file"]["tmp_name"],
    "../pending/" . $_POST["user"] . "-" . $_POST["mapname"]);
  echo "Replaced: " . $_POST["user"] . "-" . $_POST["mapname"] . " successfully.<br>";
  } else {
    move_uploaded_file($_FILES["file"]["tmp_name"],
    "C:/CMRmaps/36/pending/" . $_POST["user"] . "-" . $_POST["mapname"]);
  echo "Uploaded: " . $_POST["user"] . "-" . $_POST["mapname"] . " successfully.<br>";
  }
}

?>