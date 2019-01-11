**Introduction**

This plugin provides a feature to allow you to use the Semantex ReSearcher SOLR extension within Telligent.

**Installation**

Download https://jar-download.com/artifacts/com.sematext.solr/st-ReSearcher-core/1.12.6.3.0/source-code and update SOLR with these files

Update SOLR 

In the solr.config file in the Telligent-Content core configuration 

Update the request handler for select to this

```xml
  <requestHandler name="/select" class="solr.SearchHandler">
    <!-- default values for query parameters can be specified, these
         will be overridden by parameters in the request
      -->
     <lst name="defaults">
       <str name="df">text</str>
       <str name="echoParams">explicit</str>
       <str name="defType">edismax</str>
       <str name="lowercaseOperators">false</str>  <!-- Set to true if you  "and" and "or" should be treated the same as operators "AND" and "OR". -->
       <str name="q.alt">*:*</str>
       <str name="rows">10</str>
       <str name="fl">*,score</str>
       <str name="mm">2&lt;2 3&lt;75% 9&lt;66%</str>  <!-- 1-2 -> all terms required; 3 -> 2 terms required; 4-9 -> 75% required; 10+ ->66% required-->
						   
       <str name="qf">
          title^1.2 content^1.0 attachmenttext^1.0 tagtext^1.0				  
       </str>
						   

       <!-- Highlighting defaults -->
       <str name="hl.fl">title content</str>
	   
	   <!-- Spell checking defaults -->
       <str name="spellcheck">on</str>
       <str name="spellcheck.dictionary">KMChecker</str>
       <str name="spellcheck.extendedResults">false</str>       
       <str name="spellcheck.count">5</str>
       <str name="spellcheck.alternativeTermCount">2</str>
       <str name="spellcheck.maxResultsForSuggest">5</str>       
       <str name="spellcheck.collate">true</str>
       <str name="spellcheck.collateExtendedResults">true</str>  
       <str name="spellcheck.maxCollationTries">5</str>
       <str name="spellcheck.maxCollations">3</str>  
	   
       <str name="queryRelaxer">true</str>
		<str name="queryRelaxer.rows">1</str>
		<str name="queryRelaxer.rowsPerQuery">1</str>
		<str name="queryRelaxer.maxQueries">3</str>
		<str name="queryRelaxer.longQueryTerms">5</str>
		<str name="queryRelaxer.longQueryMM">75%</str>
		<str name="queryRelaxer.preferFewerMatches">true</str>
     </lst>
	 
	  <arr name="last-components">
       <str>relaxerComponent</str>		 -
       <str>spellcheck</str>
     </arr>

    </requestHandler>
```

And add this to the bottom of the file

```xml
 <searchComponent name="spellcheck" class="solr.SpellCheckComponent">

		<lst name="spellchecker">
			<!--
				Optional, it is required when more than one spellchecker is configured.
				Select non-default name with spellcheck.dictionary in request handler.
			-->
			<str name="name">default</str>
			<!-- The classname is optional, defaults to IndexBasedSpellChecker -->
			<str name="classname">solr.IndexBasedSpellChecker</str>
			<!--
				Load tokens from the following field for spell checking,
				analyzer for the field's type as defined in schema.xml are used
			-->
			<str name="field">content</str>
			<!-- Set the accuracy (float) to be used for the suggestions. Default is 0.5 -->
			<str name="accuracy">0.5</str>
			<!-- Require terms to occur in 1/100th of 1% of documents in order to be included in the dictionary -->
			<float name="thresholdTokenFrequency">.0001</float>
		</lst>
		<!-- a spellchecker that can break or combine words. (Solr 4.0 see SOLR-2993) -->
		<lst name="spellchecker">
			<str name="name">wordbreak</str>
			<str name="classname">solr.WordBreakSolrSpellChecker</str>
			<str name="field">content</str>
			<str name="combineWords">true</str>
			<str name="breakWords">true</str>
			<int name="maxChanges">3</int>
		</lst>
		<lst name="spellchecker">
			<str name="name">KMChecker</str>
			<str name="field">content</str>
			<str name="classname">solr.DirectSolrSpellChecker</str>
			<str name="distanceMeasure">internal</str>
			<int name="maxEdits">2</int>
			<int name="minPrefix">1</int>
			<int name="maxInspections">5</int>
			<int name="minQueryLength">4</int>
			<float name="accuracy">0.5</float>
			<float name="maxQueryFrequency">0.01</float>
			</lst>
		
		
		<!-- This field type's analyzer is used by the QueryConverter to tokenize the value for "q" parameter -->
		<!--<str name="queryAnalyzerFieldType">textSpell</str> -->
		<str name="queryAnalyzerFieldType">spelling_text</str>
	</searchComponent>

	<!-- SEMATEXT COMMENT : Definition of DymReSearcher component instance which uses First Good Suggestion
         algorithm. This component may be used in multiple search handler definitions. If DymReSearcher component 
         shouldn't be used at all, remove this definition, as well as all occurrences of this component in
         last-components lists of search handlers. -->      
   
   <searchComponent name="relaxerComponent" class="com.sematext.solr.handler.component.relaxer.QueryRelaxerComponent">
		<int name="maxOriginalResults">0</int>
		<arr name="phraseQueryHeuristics">
			<str>com.sematext.solr.handler.component.relaxer.heuristics.RemoveOneClauseHeuristic</str>
		</arr>
		<arr name="regularQueryHeuristics">
			<str>com.sematext.solr.handler.component.relaxer.heuristics.RemoveOneClauseHeuristic</str>
		</arr>
   </searchComponent>
```

When you start SOLR and perform a query you should see additional meta data in the search repsonse.

Finally once SOLR is configured

Install the binaries into the website and restarting, the search page layout can be updated with the new Search Suggestions Widget


