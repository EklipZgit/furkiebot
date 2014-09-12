<html>
<body>
FK U I DO WHAT I WANT<br>
<form action="upload_file.php" method="post"
enctype="multipart/form-data">
<label for="file">Filename:</label>
<input type="file" name="file" id="file"><br>
<input type="submit" name="submit" value="Submit">
</form>

</body>
</html>


<?php
$temp = explode(".", $_FILES["file"]["name"]);


if ($_FILES["file"]["error"] > 0) {
  echo "Return Code: " . $_FILES["file"]["error"] . "<br>";
} else {
  echo "Upload: " . $_FILES["file"]["name"] . "<br>";
  echo "Type: " . $_FILES["file"]["type"] . "<br>";
  echo "Size: " . ($_FILES["file"]["size"] / 1024) . " kB<br>";
  echo "Temp file: " . $_FILES["file"]["tmp_name"] . "<br>";
  if (file_exists("data/" . $_FILES["file"]["name"])) {
    echo $_FILES["file"]["name"] . " already exists. ";
  } else {
    move_uploaded_file($_FILES["file"]["tmp_name"],
    "data/" . $_FILES["file"]["name"]);
    echo "Stored in: " . "data/" . $_FILES["file"]["name"];
  }
}

?>