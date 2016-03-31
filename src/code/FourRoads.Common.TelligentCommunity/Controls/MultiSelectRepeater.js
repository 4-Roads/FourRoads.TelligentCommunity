	function MultiselectDG_Row(orginalClassName , dataSourceID , rowID)
	{
		this.orginalClassName = orginalClassName; 
		this.dataSourceID = dataSourceID;
		this.rowID = rowID;
	}
	
	function MultiselectDG_RowManager(multiSelect , selectedItemIndexesHiddenID , highlightClass , selectedClass , selectedItemCallback)
	{
		this.multiSelect = multiSelect;
		this.highlightClass = highlightClass;
		this.selectedClass = selectedClass;
		this.selectedItemIndexesHidden = document.getElementById(selectedItemIndexesHiddenID);
		this.rows = new Array();
		this.selectedItemCallback = selectedItemCallback;
		
		this.AddRow = function(rowDetails)
		{
			var i = this.rows.length;
			this.rows[i] = rowDetails;
		}									
		
		this.MouseRowIn = function(index)
		{
			row = this.GetRow(this.rows[index]);
			
			if (row != null)
			{
				row.className = this.highlightClass;
			}
		}

		this.MouseRowOut = function(index)
		{
			row = this.GetRow(this.rows[index]);
			
			if (row != null)
			{
				if (this._rowIsSelected(index))
				{
					row.className =	 this.selectedClass;
				}
				else
				{
					row.className = this.rows[index].orginalClassName;
				}
			}
		}
		
		this.SelectRow = function(e , index)
		{
			if (this._rowIsSelected(index))
			{
				this._unselectRow(index);
			}

			//If single select mode or the ctrl key is not pressed
			if (this.multiSelect == false || !this._ctrlPressed(e))
			{
				this._clearCurrentSelections();
			}
		
			row = this.GetRow(this.rows[index]);
		
			if (row != null)
			{
				row.className =	 this.selectedClass;
				this.selectedItemIndexesHidden.value += '^' + this.rows[index].dataSourceID;
				
				if (this.selectedItemCallback != null)
					this.selectedItemCallback(index , this.rows[index].dataSourceID , true);
			}
		}
		
		this._ctrlPressed =function(e)
		{
			var evtobj=window.event? event : e;
			
			return evtobj.ctrlKey;
		}
			
		this._rowIsSelected = function(index)
		{
			//Search the hidden control to find if the rows data source id is in the list
			var items = this.selectedItemIndexesHidden.value.split('^');
			
			var searchFor =	this.rows[index].dataSourceID;
			
			for(i = 0; i < items.length; i++)
			{
				if (items[i] == searchFor)
				{
					return true;
				}
			}
		
			return false;
		}
		
		this.SelectedItems = function()
		{
		    return this.selectedItemIndexesHidden.value.split('^');
		}
		
		this._clearCurrentSelections = function()
		{
			for(i = 0; i < this.rows.length; i++)
			{
				row = this.GetRow(this.rows[i]);
				
				row.className = this.rows[i].orginalClassName;
			}
			
			//Clear the hidden selection control
			this.selectedItemIndexesHidden.value = '';
		}
		
		this._unselectRow = function(index)
		{
			row = this.GetRow(this.rows[index]);
			
			if (row != null)
			{
				row.className = this.rows[index].orginalClassName;			
			}

			//Now itterate over the hidden control values and remove this one
			var items = this.selectedItemIndexesHidden.value.split('^');
			var searchFor =	this.rows[index].dataSourceID;
			
			var moreItems = false;
			
			this.selectedItemIndexesHidden.value = '';
			for(i = 0; i < items.length; i++)
			{
				if (items[i] != searchFor && items[i] != '')
				{
				   moreItems = true;
				   this.selectedItemIndexesHidden.value += '^' + items[i];
				}
			}
			
			if (this.selectedItemCallback != null)
				this.selectedItemCallback(index , this.rows[index].dataSourceID , moreItems);
		}
		
		this.GetRow = function(rowData)
		{
			return  document.getElementById(rowData.rowID);
		}
	}
