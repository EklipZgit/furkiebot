<?php
include_once "funcs.php";




/**
 * MapData class to represent maps. Should always match the one in FurkieBot.
 */
class MapData {
    public $name = "";
    public $id = -1;
    public $filepath = "";
    public $author = "";
    public $acceptedBy = "";
    public $accepted = false;

    public function __construct(Array $loadedArray = array()) {

		foreach($loadedArray as $key => $value){
			$this->{$key} = $value;
		}
    }
}

?>