/*
 *  $Id: GedcomRecordReader.cs 200 2008-11-30 14:34:07Z davek $
 * 
 *  Copyright (C) 2007-2008 David A Knight <david@ritter.demon.co.uk>
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA
 *
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

// TODO merge duplicated phone, fax, email, url, address processing & logic (at least 5 copies?)

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable SuggestUseVarKeywordEvident

namespace GEDCOM.Net
{
	/// <summary>
	/// GedcomRecordReader will read in a given gedcom file
	/// producing a GedcomDatabase and related objects.
	/// This is generally what you want to use to read in a GEDCOM
	/// file for manipulation.
	/// </summary>
	public class GedcomRecordReader
	{
		#region Variables
		
		private GedcomParser _Parser;
		private GedcomParseState _ParseState;
		private string _gedcomFile;
	    private string _gedcomPath;
		
		private XRefIndexedKeyCollection _xrefCollection;
		
		private int _percent;

		private List<string> _missingReferences;

		private List<GedcomSourceCitation> _sourceCitations;
		private List<GedcomRepositoryCitation> _repoCitations;
		
		private List<string> _removedNotes;
		
		private int _level;
		private string _tag;
		private string _xrefID;
		private string _lineValue;
		private GedcomLineValueType _lineValueType;
		
		private StreamReader _stream;
		
		#endregion
		
		#region Constructors
		
		/// <summary>
		/// Create a GedcomRecordReader for reading a GEDCOM file into a GedcomDatabase
		/// </summary>
		public GedcomRecordReader()
		{
			_Parser = new GedcomParser();

			// we don't care if delims are multiple spaces
			_Parser.IgnoreInvalidDelim = true;

			// we don't care if lines are missing delimeters
			_Parser.IgnoreMissingTerms = true;
			
			// apply hack for lines that are just part of the line value
			// for the previous CONC/CONT in invalid GEDCOM files
			_Parser.ApplyConcContOnNewLineHack = true;

			// allow tabs in line values, seen from RootsMagic and GenealogyJ
			_Parser.AllowTabs = true;

			// allow line tabs in line values, seen from Legacy
			_Parser.AllowLineTabs = true;
			
			// allow information separator one chars, seen from that bastion
			// of spec compliance RootsMagic
			_Parser.AllowInformationSeparatorOne = true;

			// allow - or _ in tag names (GenealogyJ?)
			_Parser.AllowHyphenOrUnderscoreInTag = true;
			
			_Parser.ParserError += Parser_ParseError;
			_Parser.TagFound += Parser_TagFound;
		}
		
		#endregion
		
		#region Properties
		
		/// <value>
		/// The parser to be used when reading the GEDCOM file 
		/// </value>
		public GedcomParser Parser
		{
			get { return _Parser; }
			set { _Parser = value; }
		}

		/// <value>
		/// The GEDCOM file being read
		/// </value>
		public string GedcomFile
		{
			get { return _gedcomFile; }
			set
			{
			    _gedcomFile = value;
                _gedcomPath = Directory.GetParent(_gedcomFile).FullName;
            }
		}

		/// <value>
		/// The database the records will be added to
		/// </value>
		public GedcomDatabase Database
		{
			get { return _ParseState.Database; }	
		}

		/// <value>
		/// When reading GEDCOM files into a database the
		/// xref ids may already exist, settings this to true
		/// will cause new ids to be generated created for the
		/// records being read.
		/// </value>
		public bool ReplaceXRefs
		{
			get { return _xrefCollection.ReplaceXRefs; }
			set { _xrefCollection.ReplaceXRefs = value; }
		}

		/// <value>
		/// Percentage progress of GedcomRead
		/// </value>
		public int Progress
		{
			get { return _percent; }
		}
		
		#endregion

		#region Events

		/// <summary>
		/// Fired as each line is parsed from the given file in GedcomRead
		/// </summary>
		public event EventHandler PercentageDone;
		
		#endregion
		
		#region Event Handlers
		
		private void Parser_ParseError(object sender, EventArgs e)
		{		
			string error = GedcomParser.GedcomErrorString(_Parser.ErrorState);
			Debug.WriteLine(error);
			Console.WriteLine(error);
		}
		
		private void Parser_TagFound(object sender, EventArgs e)
		{
			_level = _Parser.Level;
			_xrefID = _Parser.XrefID;
			_tag = TagMap(_Parser.Tag);
			_lineValue = _Parser.LineValue;
			_lineValueType = _Parser.LineValueType;

		    // pop previous levels from the stack
			
			GedcomRecord current = PopStack(_level);
			
			if (current == null)
			{
				switch (_tag)
				{
					case "FAM":
						
						// must have an xref id to have a family record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomFamilyRecord();
						}
						break;
					case "INDI":
						
						// must have an xref id to have an individual record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomIndividualRecord();
						}
						break;
					case "OBJE":
						
						// must have an xref id to have a multimedia record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomMultimediaRecord();
						}
						break;
					case "NOTE":
						
						// must have an xref id to have a note record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							GedcomNoteRecord note = new GedcomNoteRecord();
							current = note;
							
							// set initial note text if needed
							
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								note.ParsedText.Append(_lineValue);
							}
							else if (_lineValue != string.Empty)
							{
								// pointer to a note, this should not occur
								// as we should be at level 0 here
								
								Debug.WriteLine("Spurious Note pointer: " + _xrefID + "\t at level: " + _level);
							}
						}
						break;
					case "REPO":
						
						// must have an xref id to have a repository record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomRepositoryRecord();
						}
						break;
					case "SOUR":
						
						// must have an xref id to have a source record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomSourceRecord();
						}
						break;
					case "SUBM":
						
						// must have an xref id to have a submitter record
						// otherwise it can't be referenced anywhere
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomSubmitterRecord();
                            if (Database.Header != null)
                            {
                                Database.Header.AddSubmitter(current as GedcomSubmitterRecord);
                            }
                        }
						break;
					case "HEAD":
						
						// header record
						current = new GedcomHeader();
					
						break;

					case "SUBN":

						// Submission record
						if (!string.IsNullOrEmpty(_xrefID))
						{
							current = new GedcomSubmissionRecord();
						}
						break;
						
					case "TRLR":
						
						break;
					default:
						
						// Unknown tag
						
						Debug.WriteLine("Unknown: " + _tag + " at level: " + _level);
						break;
				}
				
				// if we created a new record push it onto the stack
				if (current != null)
				{
					if (!string.IsNullOrEmpty(_xrefID))
					{
						current.XRefID = _xrefID;
					}
					current.Database = _ParseState.Database;
					current.Level = _level;
					_ParseState.Records.Push(current);
				}
			}
			else
			{
				switch (current.RecordType)
				{
					case GedcomRecordType.Header:
						ReadHeaderRecord();
						break;
					case GedcomRecordType.Family:
						ReadFamilyRecord();
						break;
					case GedcomRecordType.Individual:
						ReadIndividualRecord();
						break;
					case GedcomRecordType.Multimedia:
						ReadMultimediaRecord();
						break;
					case GedcomRecordType.Note:
						ReadNoteRecord();
						break;
					case GedcomRecordType.Repository:
						ReadRepositoryRecord();
						break;
					case GedcomRecordType.Source:
						ReadSourceRecord();
						break;
					case GedcomRecordType.Submitter:
						ReadSubmitterRecord();
						break;
					case GedcomRecordType.Submission:
						ReadSubmissionRecord();						
						break;
					
					// Non top level records
					case GedcomRecordType.Event:
						ReadEventRecord();
						break;
					case GedcomRecordType.FamilyEvent:
						ReadEventRecord();
						break;
					case GedcomRecordType.IndividualEvent:
						ReadEventRecord();
						break;
					
					case GedcomRecordType.Place:
						ReadPlaceRecord();
						break;
					case GedcomRecordType.SourceCitation:
						ReadSourceCitationRecord();
						break;
					case GedcomRecordType.FamilyLink:
						ReadFamilyLinkRecord();
						break;
					case GedcomRecordType.Association:
						ReadAssociationRecord();
						break;
					case GedcomRecordType.Name:
						ReadNameRecord();
						break;
					case GedcomRecordType.Date:
						ReadDateRecord();
						break;
					case GedcomRecordType.RepositoryCitation:
						ReadRepositoryCitation();
						break;
					case GedcomRecordType.CustomRecord:
						ReadEventRecord();
						break;
				}
			}
			
			_ParseState.AddPreviousTag(_tag, _level);
		}
		
		#endregion
		
		#region Methods
		
		private GedcomRecord PopStack(int level)
		{
			GedcomRecord current = null;
			
			if (_ParseState.Records.Count != 0)
			{
				current = _ParseState.Records.Peek();	
			}

			while ((_ParseState.PreviousTags.Count > 0) &&
				   (_ParseState.PreviousTags.Peek().Second >= level))
			{
				_ParseState.PreviousTags.Pop();
			}
			
			while (current != null && level <= current.ParsingLevel)
			{
				switch (current.RecordType)
				{
					// hack for ADDR appearing on INDI, done by Family Tree Maker
					// convert it to a RESI
					case GedcomRecordType.Individual:
						GedcomIndividualRecord indi = (GedcomIndividualRecord)current;
						GedcomAddress address = indi.Address;
						
						if (address != null)
						{
							GedcomIndividualEvent resi = new GedcomIndividualEvent();
							resi.EventType = GedcomEvent.GedcomEventType.RESIFact;
							resi.Database = Database;
							resi.Level = indi.Level + 1;
							resi.IndiRecord = indi;
							resi.Address = address;

							indi.Events.Add(resi);
							
							indi.Address = null;	
						}
						break;
				
					// hacks to avoid allocating lots of strings, we use a string builder
					// as we have hit the end of the record set the value from the string builder
					case GedcomRecordType.Note:
						GedcomNoteRecord note = (GedcomNoteRecord)current;
						note.Text = note.ParsedText.ToString();
						note.ParsedText = null;

                        if (note.Text.Trim().Length == 0)
						{
							_removedNotes.Add(note.XRefID);
							current = null;
						}
						break;
					case GedcomRecordType.SourceCitation:
						GedcomSourceCitation citation = (GedcomSourceCitation)current;
						if (citation.ParsedText != null)
						{
							citation.Text = citation.ParsedText.ToString();
							citation.ParsedText = null;
						}
						break;
					case GedcomRecordType.Source:
						GedcomSourceRecord source = (GedcomSourceRecord)current;
						if (source.TitleText != null)
						{
							source.Title = source.TitleText.ToString();
							source.TitleText = null;
						}
						else if (source.OriginatorText != null)
						{
							source.Originator = source.OriginatorText.ToString();
							source.OriginatorText = null;
						}
						else if (source.PublicationText != null)
						{
							source.PublicationFacts = source.PublicationText.ToString();
							source.PublicationText = null;
						}
						else if (source.TextText != null)
						{
							source.Text = source.TextText.ToString();
							source.TextText = null;
						}
						break;
				}
				
				if (current != null)
				{
					// ensure database is set on all records
					// or at least those that have been pushed on the stack
                    current.Database = Database;
					
					// pop as we are at a higher level now
					if (current.Level == 0 && current.RecordType != GedcomRecordType.Header)
					{
						_ParseState.Database.Add(current.XRefID,current);
					}
					
					current = null;
				}				
				
				_ParseState.Records.Pop();
				
				if (_ParseState.Records.Count > 0)
				{
					current = _ParseState.Records.Peek();	
				}
			}
			
			return current;
		}
		
		/// <summary>
		/// Starts reading the gedcom file currently set via the GedcomFile property
		/// </summary>
		/// <returns>bool indicating if the file was successfully read</returns>
		public bool ReadGedcom()
		{
			return ReadGedcom(_gedcomFile);	
		}

		/// <summary>
		/// Starts reading the specified gedcom file
		/// </summary>
		/// <param name="gedcomFile">Filename to read</param>
		/// <returns>bool indicating if the file was successfully read</returns>
		public bool ReadGedcom(string gedcomFile)
		{
		    GedcomFile = gedcomFile;
			
			_percent = 0;

			FileInfo info = new FileInfo(gedcomFile);
			long fileSize = info.Length;
			long read = 0;
			
			_missingReferences = new List<string>();
			_sourceCitations = new List<GedcomSourceCitation>();
			_repoCitations = new List<GedcomRepositoryCitation>();
			
			try
			{
				_stream = null;
				Encoding enc = Encoding.Default;
				
				using (FileStream fileStream = File.OpenRead(gedcomFile))
				{			
					ResetParse();
			
					byte[] bom = new byte[4];
				
					fileStream.Read(bom, 0, 4);
				
					// look for BOMs, if found we will ignore the CHAR tag
					// don't use .net look for bom as we also want to detect
					// unicode where there isn't a BOM, as far as the parser
					// is concerned the data is utf16le if we detect this way
					// as the conversion is already done
				
					if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.UTF8;
					}
					else if (bom[0] == 0xFE && bom[1] == 0xFF)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.BigEndianUnicode;
					}
					else if (bom[0] == 0xFF && bom[1] == 0xFE && bom[2] == 0x00 && bom[3] == 0x00)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.UTF32;
					}
					else if (bom[0] == 0xFF && bom[1] == 0xFE)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.Unicode;
					}
					else if (bom[0] == 0x00 && bom[1] == 0x00 && bom[2] == 0xFE && bom[3] == 0xFF)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.UTF32;
					}
					else if (bom[0] == 0x00 && bom[2] == 0x00)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.BigEndianUnicode;
					}
					else if (bom[1] == 0x00 && bom[3] == 0x00)
					{
						_Parser.Charset = GedcomCharset.UTF16LE;
						enc = Encoding.Unicode;
					}
				}
							
				_stream = new StreamReader(gedcomFile, enc);
					
				while (!_stream.EndOfStream)
				{
					string line = _stream.ReadLine();

					if (line != null)
					{
						// file may not have same newline as environment so this isn't 100% correct
						read += line.Length + Environment.NewLine.Length;
						_Parser.GedcomParse(line);
						line = null;
						
						// to allow for inaccuracy above
						int percentDone = (int)Math.Min(100, (read * 100.0F)/fileSize);
						if (percentDone != _percent)
						{
							_percent = percentDone;
							if (PercentageDone != null)
							{
								PercentageDone(this, EventArgs.Empty);
							}
						}
					}
				}
				Flush();
			}
			finally
			{
				if (_stream != null)
				{
					_stream.Dispose();	
				}
			}

			bool success = (_Parser.ErrorState == GedcomErrorState.NoError);
			
			if (success)
			{
				_percent = 100;

				// cleanup header record, don't want submitter record or content description in the main
				// database submitters / notes
				GedcomHeader header = Database.Header;
				
				if (header != null)
				{
					if (header.Notes.Count > 0)
					{
						string xref = header.Notes[0];

						// belongs in content description, not top level record notes
					    header.Notes.Remove(xref);
						header.ContentDescription = (GedcomNoteRecord)Database[xref];

						// fix up level, note is inline in the header + remove from database
						// list of notes
						header.ContentDescription.Level = 1;
						header.ContentDescription.XRefID = string.Empty;
						Database.Remove(xref, header.ContentDescription);
						
					}

					// brothers keeper doesn't output a source name, so set the name to
					// the same as the ID if it is empty
					if (string.IsNullOrEmpty(header.ApplicationName) && !string.IsNullOrEmpty(header.ApplicationSystemID))
					{
						header.ApplicationName = header.ApplicationSystemID;
					}
				}
				
				// add any missing child in and spouse in linkage
				foreach (GedcomFamilyRecord family in Database.Families)
				{
					string husbandID = family.Husband;
					if (!string.IsNullOrEmpty(husbandID))
					{
						GedcomIndividualRecord husband = Database[husbandID] as GedcomIndividualRecord;
						if (husband != null)
						{
							GedcomFamilyLink famLink;
							
							if (!husband.SpouseInFamily(family.XRefID, out famLink))
							{
								famLink = new GedcomFamilyLink();
								famLink.Database = Database;
								famLink.Family = family.XRefID;
								famLink.Indi = husbandID;
								famLink.Level = 1;
								famLink.PreferedSpouse = (husband.SpouseIn.Count == 0);
								husband.SpouseIn.Add(famLink);
							}
						}
						else
						{
							Debug.WriteLine("Husband in family points to non individual record");	
						}
					}
					
					string wifeID = family.Wife;
					if (!string.IsNullOrEmpty(wifeID))
					{
						GedcomIndividualRecord wife = Database[wifeID] as GedcomIndividualRecord;
						if (wife != null)
						{
							GedcomFamilyLink famLink;
							
							if (!wife.SpouseInFamily(family.XRefID, out famLink))
							{
								famLink = new GedcomFamilyLink();
								famLink.Database = Database;
								famLink.Family = family.XRefID;
								famLink.Indi = wifeID;
								famLink.Level = 1;
								wife.SpouseIn.Add(famLink);
							}
						}
						else
						{
							Debug.WriteLine("Wife in family points to non individual record");	
						}
					}
					
					foreach (string childID in family.Children)
					{	
						GedcomIndividualRecord child = Database[childID] as GedcomIndividualRecord;
					
						if (child != null)
						{
							GedcomFamilyLink famLink;
							
							// add a family link record if one doesn't already exist
							if (!child.ChildInFamily(family.XRefID, out famLink))
							{		
								famLink = new GedcomFamilyLink();
								famLink.Database = Database;
								famLink.Family = family.XRefID;
								famLink.Indi = childID;
								famLink.Level = 1;
								famLink.Status = ChildLinkageStatus.Unknown;
								// pedigree now set below
								child.ChildIn.Add(famLink);
							}
							
							// set pedigree here to allow for ADOP/FOST in the FAM tag
							// FAM record overrides link status if they differ
							famLink.Pedigree = family.GetLinkageType(childID);
							famLink.FatherPedigree = family.GetHusbandLinkageType(childID);
							famLink.MotherPedigree = family.GetWifeLinkageType(childID);
							
							// check BIRT event for a FAMC record, check ADOP for FAMC / ADOP records
							foreach (GedcomIndividualEvent indiEv in child.Events)
							{
								if (indiEv.Famc == family.XRefID)
								{
									switch (indiEv.EventType)
									{
										case GedcomEvent.GedcomEventType.BIRT:
											// BIRT records do not state father/mother birth,
											// all we can say is both are natural
											famLink.Pedigree = PedegreeLinkageType.Birth;
											break;
										case GedcomEvent.GedcomEventType.ADOP:
											switch (indiEv.AdoptedBy)
											{
												case GedcomAdoptionType.Husband:
													famLink.FatherPedigree = PedegreeLinkageType.Adopted;
													break;
												case GedcomAdoptionType.Wife:
													famLink.MotherPedigree = PedegreeLinkageType.Adopted;
													break;
												case GedcomAdoptionType.HusbandAndWife:
												default:
													// default is both as well, has to be adopted by someone if
													// there is an event on the family.
													famLink.Pedigree = PedegreeLinkageType.Adopted;
													break;
											}
											break;
									}
								}
							}
						}
						else
						{
							Debug.WriteLine("Child in family points to non individual record");	
						}
					}
					family.ClearLinkageTypes();
				}
			
				// look for any broken references / update ref counts
				foreach (string xref in _missingReferences)
				{
					GedcomRecord record = Database[xref];
					if (record != null)
					{
						switch (record.RecordType)
						{
							case GedcomRecordType.Individual:
								// FIXME: don't increase ref count on individuals,
								// a bit of a hack, only place where it may be
								// needed is on assocciations
								break;
							case GedcomRecordType.Family:
								// FIXME: don't increase ref count on families
								break;
							default:
								record.RefCount ++;
								break;
						}
					}
					else if (!_removedNotes.Contains(xref))
					{
						Debug.WriteLine("Missing reference: " + xref);
					}
				}
				Console.WriteLine("Removed " + _removedNotes.Count + " notes");
				_missingReferences = null;
			
				// link sources with citations which reference them
				foreach (GedcomSourceCitation citation in _sourceCitations)
				{
					GedcomSourceRecord source = Database[citation.Source] as GedcomSourceRecord;
					if (source != null)
					{
						source.Citations.Add(citation);
					}
					else
					{
						Debug.WriteLine("Missing source reference: " + citation.Source);
					}
				}
				_sourceCitations = null;
				
				// link repos with citations which reference them
				foreach (GedcomRepositoryCitation citation in _repoCitations)
				{
					GedcomRepositoryRecord repo = Database[citation.Repository] as GedcomRepositoryRecord;
					if (repo != null)
					{
						repo.Citations.Add(citation);
					}
					else
					{
						Debug.WriteLine("Missing repo reference: " + citation.Repository);
					}
				}
				_repoCitations = null;
	
				// find any sources without a title and give them one, happens with Database1.ged,
				// could be bad parsing, not sure, try and make up for it anyway
				int missingSourceTitleCount = 1;
				foreach (GedcomSourceRecord source in Database.Sources)
				{
					if (string.IsNullOrEmpty(source.Title))
					{
						source.Title = string.Format("Source {0}", missingSourceTitleCount ++);
					}
				}
				
				Database.Name = gedcomFile;
			}
			
			if (PercentageDone != null)
			{
				PercentageDone(this, EventArgs.Empty);
			}

			Database.Loading = false;
			
			return success;
		}
		
		private void ResetParse()
		{
			// set specialist IndexedKeyCollection that supports replacing xrefs
			_xrefCollection = new XRefIndexedKeyCollection();
			// always replace xrefs
			_xrefCollection.ReplaceXRefs = true;
			_Parser.XrefCollection = _xrefCollection;
			
			_Parser.ResetParseState();
			_ParseState = new GedcomParseState();
			_xrefCollection.Database = _ParseState.Database;
			_missingReferences = new List<string>();
			_sourceCitations = new List<GedcomSourceCitation>();
			_repoCitations = new List<GedcomRepositoryCitation>();
		
			_removedNotes = new List<string>();
			
			Database.Loading = true;
		}
		
		private void Flush()
		{
			// process / clean up anything left in the parser
			PopStack(0);
		}
				
		private bool AddressParse(GedcomAddress address, string tag, string lineValue, GedcomLineValueType lineValueType)
		{
			bool done = false;
						
			//  FIXME: checking for ADDR is wrong, doesn't work properly, ok to just
			//  check address is not null?  Real solution is to use a stack for PreviousTag
			// like it should have been doing in the first place
			// PreviousTag is now using a stack so will return the parent tag, which should be ADDR
            // TODO fix this
			if (address != null || _ParseState.PreviousTag == "ADDR")
			{
				switch (tag)
				{
					case "CONT":
						address.AddressLine += Environment.NewLine;
						address.AddressLine += lineValue;
						done = true;
						break;
					case "ADR1":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.AddressLine1 = lineValue;
						}
						done = true;
						break;
					case "ADR2":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.AddressLine2 = lineValue;
						}
						done = true;
						break;
					case "ADR3":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.AddressLine3 = lineValue;
						}
						done = true;
						break;
					case "CITY":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.City = lineValue;
						}
						done = true;
						break;
					case "STAE":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.State = lineValue;
						}
						done = true;
						break;
					case "POST":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.PostCode = lineValue;
						}
						done = true;
						break;
					case "CTRY":
						if (lineValueType == GedcomLineValueType.DataType)
						{
							address.Country = lineValue;
						}
						done = true;
						break;
				}
			}
			
			return done;
		}
		
		private void DateParse(GedcomDate date, string lineValue)
		{			
			date.ParseDateString(lineValue);
			
			// no parsed date, perhaps it was an age?
			if (date.DateTime1 == null)
			{
				// date handling is severly broken in genealogy applications,
				// with many not taking any notice of the mandated formats when
				// outputting gedcom, and some such as Family Tree Maker
				// inserting what belongs in AGE as the date, e.g. INFANT

				// this is the date record
				GedcomRecord record = _ParseState.Records.Pop();
				
				// this is the one we are interested in
				record = _ParseState.Records.Peek();
				
				// put the date record back
				_ParseState.Records.Push(date);

				GedcomIndividualEvent ev = record as GedcomIndividualEvent;
				if (ev != null)
				{
					GedcomAge age = GedcomAge.Parse(lineValue, Database);
					if (age != null)
					{
						// we have a valid age, could calc a date at some point
						// based off birth of individual, don't do that here though
						
						// don't clear lineValue, we need something to keep
						// the event active!
						
						ev.Age = age;
					}
				}
			}
		}
		
		private void ReadHeaderRecord()
		{
			if (_tag.StartsWith("_"))
			{
				switch (_tag)
				{
					default:
						GedcomCustomRecord custom = new GedcomCustomRecord();
						custom.Level = _level;
						custom.XRefID = _xrefID;
						custom.Tag = _tag;
									
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							custom.Classification = _lineValue;
						}
						
						// FIXME: may want to use customs at some point
		
						_ParseState.Records.Push(custom);
						break;
				}
			}

            GedcomHeader headerRecord = _ParseState.Records.Peek() as GedcomHeader;
            if (headerRecord == null)
                return;
			
			if (_level == headerRecord.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "CHAR":
						// special case to get the character set we should be using
						// only do if charset is unknown or we will get in a nice loop
						if (_Parser.Charset == GedcomCharset.Unknown)
						{
							Encoding enc = null;
							GedcomCharset charset = GedcomCharset.UnSupported;
							switch (_lineValue)
							{
								case "ANSEL":
									charset = GedcomCharset.Ansel;
									enc = new AnselEncoding();
									break;
								case "ANSI":
									charset = GedcomCharset.Ansi;
									// default to windows codepage, wrong but best guess
									// or should it be 436 (DOS)
									enc = Encoding.GetEncoding(1252);
									break;
								case "IBMPC":
									enc = Encoding.GetEncoding(437);
									break;
								case "UTF8":
									// this is correct, we will already have converted from utf8
									charset = GedcomCharset.UTF16LE;
									break;
								case "ASCII":
									// yes, ASCII is the same as UTF8 but extended ascii spoils that
									// which is probably in use
                                    // KBR 01/23/2013: No ASCII is *not* the same as UTF8. ASCII in StreamReader is 7-bit
                                    // characters, probably *not* what is desired. Use ANSI instead...
									charset = GedcomCharset.Ascii;
									enc = Encoding.ASCII;
							        enc = Encoding.GetEncoding(1252);
									break;
								default:
									break;
							}
							if (enc != null)
							{
								_stream.Close();
								_stream.Dispose();
								_stream = new StreamReader(_gedcomFile, enc);
												
								ResetParse();
							}
						    _Parser.Charset = charset;
						}
						break;
					case "SOUR":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.ApplicationSystemID = _lineValue;
						}
						break;
					case "DEST":
						break;
					case "SUBM":
//						string submXref = 
                        AddSubmitterRecord(headerRecord);
//						headerRecord.SubmitterXRefID = submXref;
						break;
					case "SUBN":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
                            // TODO not supporting SUBN
						}
						break;
					case "COPR":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.Copyright = _lineValue;
						}
						break;
					case "LANG":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.Language = _lineValue;
						}
						break;
					case "PLAC":
						break;
					case "DATE":
						var date = new GedcomDate(Database) {Level = _level};

						_ParseState.Records.Push(date);
						headerRecord.TransmissionDate = date;
						_level ++;
						ReadDateRecord();
						_level --;
						_ParseState.Records.Pop();
						break;
					case "NOTE":
					    AddNoteRecord(headerRecord);
						break;
                    //default:
                    //    Console.WriteLine("Unhandled header tag:(" + _level + ")" + _tag);
                    //    break;
				}
			}
			else if (_level == headerRecord.ParsingLevel + 2)
			{
				switch (_tag)
				{
					case "NAME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.ApplicationName = _lineValue;	
						}
						break;
					case "VERS":
						switch (_ParseState.ParentTag(_level))
						{
							case "SOUR":
								if (_lineValueType == GedcomLineValueType.DataType)
								{
									headerRecord.ApplicationVersion = _lineValue;	
								}
								break;
							case "CHAR":
								break;
							case "GEDC":
								break;
						}
						break;
					case "CORP":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.Corporation = _lineValue;	
						}
						break;
				
					case "DATA":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.SourceName = _lineValue;
						}
						break;

                    //default:
                    //    Console.WriteLine("Unhandled header tag:(" + _level + ")" + _tag);
                    //    break;
                }
			}
			else if (_level == headerRecord.ParsingLevel + 3)
			{
				switch (_tag)
				{
					case "TIME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (headerRecord.TransmissionDate != null)
							{
								headerRecord.TransmissionDate.Time = _lineValue;
							}
						}
						break;
					case "DATE":
						GedcomDate date = new GedcomDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						headerRecord.SourceDate = date;
						_level ++;
						ReadDateRecord();
						_level --;
						_ParseState.Records.Pop();
						break;
					case "COPR":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.SourceCopyright = _lineValue;
						}
						break;
					case "ADDR":
						if (headerRecord.CorporationAddress == null)
						{
							headerRecord.CorporationAddress = new GedcomAddress();
							headerRecord.CorporationAddress.Database = Database;
						}
						
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							headerRecord.CorporationAddress.AddressLine = _lineValue;	
						}
						
						break;
					case "PHON":
						if (headerRecord.CorporationAddress == null)
						{
							headerRecord.CorporationAddress = new GedcomAddress();	
							headerRecord.CorporationAddress.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Phone1))
							{
								headerRecord.CorporationAddress.Phone1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Phone2))
							{
								headerRecord.CorporationAddress.Phone2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Phone3))
							{
								headerRecord.CorporationAddress.Phone3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 phone numbers are allowed	
							}
						}
						break;
					case "EMAIL":
						if (headerRecord.CorporationAddress == null)
						{
							headerRecord.CorporationAddress = new GedcomAddress();	
							headerRecord.CorporationAddress.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Email1))
							{
								headerRecord.CorporationAddress.Email1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Email2))
							{
								headerRecord.CorporationAddress.Email2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Email3))
							{
								headerRecord.CorporationAddress.Email3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 emails are allowed	
							}
						}
						break;
					case "FAX":
						if (headerRecord.CorporationAddress == null)
						{
							headerRecord.CorporationAddress = new GedcomAddress();	
							headerRecord.CorporationAddress.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Fax1))
							{
								headerRecord.CorporationAddress.Fax1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Fax2))
							{
								headerRecord.CorporationAddress.Fax2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Fax3))
							{
								headerRecord.CorporationAddress.Fax3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 fax numbers are allowed	
							}
						}
						break;
					case "WWW":
						if (headerRecord.CorporationAddress == null)
						{
							headerRecord.CorporationAddress = new GedcomAddress();	
							headerRecord.CorporationAddress.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Www1))
							{
								headerRecord.CorporationAddress.Www1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Www2))
							{
								headerRecord.CorporationAddress.Www2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(headerRecord.CorporationAddress.Www3))
							{
								headerRecord.CorporationAddress.Www3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 urls are allowed	
							}
						}
						break;
                    //default:
                    //    Console.WriteLine("Unhandled header tag:(" + _level + ")" + _tag);
                    //    break;
                }
			}
			else if (_level == headerRecord.ParsingLevel + 4)
			{
				AddressParse(headerRecord.CorporationAddress, _tag, _lineValue, _lineValueType);
			}
		}
		
		private void ReadFamilyRecord()
		{
		    // allowed sub records

		    var familyRecord = _ParseState.Records.Peek() as GedcomFamilyRecord;
            if (familyRecord == null)
            {
                Debug.WriteLine("Unexpected record null");
                return;
            }
			
			if (_tag.StartsWith("_"))
			{
				switch (_tag)
				{
					case "_MSTAT":
						MarriageStartStatus tmp;
                        if (Enum.TryParse(_lineValue, true, out tmp))
                            familyRecord.StartStatus = tmp;
                        else
                            Debug.WriteLine("Unknown marriage start state: " + _lineValue);
						break;
					case "_FREL":
					case "_MREL":
						if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
						    _ParseState.PreviousTag == "CHIL" &&
						    _level == _ParseState.PreviousLevel + 1)
						{
							string childID = familyRecord.Children[familyRecord.Children.Count - 1];
//							PedegreeLinkageType currentType = familyRecord.GetLinkageType(childID);
							
							GedcomAdoptionType linkTo = GedcomAdoptionType.Husband;
							if (_tag == "_MREL")
							{
								linkTo = GedcomAdoptionType.Wife;
							}
							
							switch (_lineValue)
							{
								case "Natural":
									familyRecord.SetLinkageType(childID, PedegreeLinkageType.Birth, linkTo);
									break;
								case "Adopted":
									familyRecord.SetLinkageType(childID, PedegreeLinkageType.Adopted, linkTo);
									break;
								default:
									Debug.WriteLine("Unsupported value for " + _tag + ": " + _lineValue);
									break;
							}
							break;
						}
						
						break;
					default:
						GedcomCustomRecord custom = new GedcomCustomRecord();
						custom.Level = _level;
						custom.XRefID = _xrefID;
						custom.Tag = _tag;
						
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							custom.Classification = _lineValue;
						}
						
						// FIXME: may want to use customs at some point
						//familyRecord.Events.Add(custom);
						
						_ParseState.Records.Push(custom);
						break;
				}
			}
			else if (_level == familyRecord.ParsingLevel + 1)
			{
			    GedcomFamilyEvent familyEvent;
			    switch (_tag)
				{
					case "RESN":
						
						// restriction notice
						if (_lineValueType == GedcomLineValueType.DataType)
						{
						    GedcomRestrictionNotice tmp;
                            if (Enum.TryParse(_lineValue, true, out tmp))
                                familyRecord.RestrictionNotice = tmp;
                            else
                            {
                                Debug.WriteLine("Invalid restriction type: " + _lineValue);

                                // default to confidential to protect privacy
                                familyRecord.RestrictionNotice = GedcomRestrictionNotice.Confidential;
                            }
						}
						break;
					case "ANUL":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.ANUL);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "CENS":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.CENS_FAM);				
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "DIV":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.DIV);				
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "DIVF":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.DIVF);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "ENGA":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.ENGA);				
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "MARB":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.MARB);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "MARC":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.MARC);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "MARR":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.MARR);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "MARL":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.MARL);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "MARS":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.MARS);
						_ParseState.Records.Push(familyEvent);
					
						break;	
					case "RESI":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.RESI);
						_ParseState.Records.Push(familyEvent);
					
						break;
					case "EVEN":
						
						// event
						familyEvent = familyRecord.AddNewEvent(GedcomEvent.GedcomEventType.GenericEvent);
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							familyEvent.EventName = _lineValue;
						}
										
						_ParseState.Records.Push(familyEvent);
					
						break;
					
					case "HUSB":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							familyRecord.Husband = _lineValue;
							_missingReferences.Add(_lineValue);
						}
						break;
					case "WIFE":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							familyRecord.Wife = _lineValue;
							_missingReferences.Add(_lineValue);
						}
						break;
					case "CHIL":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							familyRecord.Children.Add(_lineValue);
							_missingReferences.Add(_lineValue);
						}
						break;
					case "NCHI":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							try
							{
								familyRecord.NumberOfChildren = Convert.ToInt32(_lineValue);
							}
							catch
							{
								Debug.WriteLine("Invalid number for Number of children tag");
							}
						}
						break;
					case "SUBM":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							familyRecord.SubmitterRecords.Add(_lineValue);
							_missingReferences.Add(_lineValue);
						}
						else
						{
							GedcomSubmitterRecord submitter = new GedcomSubmitterRecord();
							submitter.Level = 0; // new top level submitter, always 0;
							submitter.ParsingLevel = _level;
							submitter.XRefID = Database.GenerateXref("SUBM");
							
							_ParseState.Records.Push(submitter);
							
							familyRecord.SubmitterRecords.Add(submitter.XRefID);
						}
						
						break;
					case "FIXME?????":
						// lds spouse sealing
						break;
					case "REFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							familyRecord.UserReferenceNumber = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							familyRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(familyRecord);
						break;
					case "SOUR":
					    AddSourceCitation(familyRecord);				
						break;
					case "OBJE":
						AddMultimediaRecord(familyRecord);
						break;
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _ParseState.PreviousTag == "REFN" &&
			            _level == _ParseState.PreviousLevel + 1)
			{
				if (_tag == "TYPE")
				{
					if (_lineValueType == GedcomLineValueType.DataType)
					{
						familyRecord.UserReferenceType = _lineValue;
					}
				}
			}
			// not valid GEDCOM, but Family Tree Maker adds ADOP/FOST tags
			// to CHIL in a FAM, this is apparently valid in GEDCOM < 5.5
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			         _ParseState.PreviousTag == "CHIL" &&
			         _level == _ParseState.PreviousLevel + 1)
			{
				string childID = familyRecord.Children[familyRecord.Children.Count - 1];
				switch (_tag)
				{
					case "ADOP":
						switch (_lineValue)
						{
							case "HUSB":
								familyRecord.SetLinkageType(childID, PedegreeLinkageType.Adopted, GedcomAdoptionType.Husband);
								break;
							case "WIFE":
								familyRecord.SetLinkageType(childID, PedegreeLinkageType.Adopted, GedcomAdoptionType.Wife);
								break;
							case "BOTH":
							default:
								familyRecord.SetLinkageType(childID, PedegreeLinkageType.Adopted);
								break;
						}
						break;
					case "FOST":
						switch (_lineValue)
						{
							case "HUSB":
								familyRecord.SetLinkageType(childID, PedegreeLinkageType.Foster, GedcomAdoptionType.Husband);
								break;
							case "WIFE":
								familyRecord.SetLinkageType(childID, PedegreeLinkageType.Foster, GedcomAdoptionType.Wife);
								break;
							case "BOTH":
							default:
								familyRecord.SetLinkageType(childID, PedegreeLinkageType.Foster);
								break;
						}
						break;
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing family node: " + _tag + "\t at level: " + _level);
			}
		}
		
		private void ReadIndividualRecord()
		{
		    GedcomIndividualRecord individualRecord = _ParseState.Records.Peek() as GedcomIndividualRecord;
            if (individualRecord == null)
                return;

		    // some custom tags we convert to generic facts/events
			// this means we have to set the line value to the type
			// they represent, so store the real line value and use
			// for the event classification.
			string customToGenericClassification = string.Empty;
			
			if (_tag.StartsWith("_"))
			{
				switch (_tag)
				{
					// we convert _MILT to EVEN Military Service
					case "_MILT":
						_tag = "EVEN";
						_lineValue = "Military Service";
						_lineValueType = GedcomLineValueType.DataType;
						break;
					// we convert _MDCL to FACT Medical
					case "_MDCL":
						_tag = "FACT";
						customToGenericClassification = _lineValue;
						_lineValue = "Medical";
						_lineValueType = GedcomLineValueType.DataType;
						break;
					// we convert _HEIG to FACT Height
					case "_HEIG":
						_tag = "FACT";
						customToGenericClassification = _lineValue;
						_lineValue = "Height";
						_lineValueType = GedcomLineValueType.DataType;
						break;
					// we convert _WEIG to FACT Weight
					case "_WEIG":
						_tag = "FACT";
						customToGenericClassification = _lineValue;
						_lineValue = "Weight";
						_lineValueType = GedcomLineValueType.DataType;
						break;
					default:
						GedcomCustomRecord custom = new GedcomCustomRecord();
						custom.Level = _level;
						custom.XRefID = _xrefID;
						custom.Tag = _tag;
									
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							custom.Classification = _lineValue;
						}
						
						// FIXME: may want to use customs at some point
						//individualRecord.Events.Add(custom);
		
						_ParseState.Records.Push(custom);
						break;
				}
			}
			if (_level == individualRecord.ParsingLevel + 1)
			{
			    GedcomIndividualEvent individualEvent;
			    switch (_tag)
				{
					case "FAMC":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							GedcomFamilyLink childIn = new GedcomFamilyLink();
							childIn.Level = _level;
							childIn.Family = _lineValue;
							childIn.Indi = individualRecord.XRefID;
							
							_missingReferences.Add(_lineValue);
							
							individualRecord.ChildIn.Add(childIn);
							_ParseState.Records.Push(childIn);
							
						}
						break;
					case "FAMS":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							GedcomFamilyLink spouseIn = new GedcomFamilyLink();
							spouseIn.Level = _level;
							spouseIn.Family = _lineValue;
							spouseIn.Indi = individualRecord.XRefID;
							spouseIn.PreferedSpouse = (individualRecord.SpouseIn.Count == 0);
							
							_missingReferences.Add(_lineValue);
							
							individualRecord.SpouseIn.Add(spouseIn);
							_ParseState.Records.Push(spouseIn);
						}
						break;
					case "ASSO":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							GedcomAssociation association = new GedcomAssociation();
							association.Level = _level;
							association.Individual = _lineValue;
							
							_missingReferences.Add(_lineValue);
							
							individualRecord.Associations.Add(association);
							_ParseState.Records.Push(association);
						}
						break;
					case "RESN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
						    GedcomRestrictionNotice tmp;
                            if (Enum.TryParse(_lineValue, true, out tmp))
                                individualRecord.RestrictionNotice = tmp;
                            else
							{
								Debug.WriteLine("Invalid restriction type: " + _lineValue);
								
								// default to confidential to protect privacy
								individualRecord.RestrictionNotice = GedcomRestrictionNotice.Confidential;
							}
						}
						break;
					case "NAME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomName name = new GedcomName();
							name.Database = _ParseState.Database;
							name.Level = _level;
							name.Name = _lineValue;
							name.PreferedName = (individualRecord.Names.Count == 0);

							individualRecord.Names.Add(name);
							_ParseState.Records.Push(name);
						}
						break;
					// Invalid, but seen from Family Origins, Family Tree Maker, Personal Ancestral File, and Legacy
					case "AKA":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomName name = new GedcomName();
							name.Database = _ParseState.Database;
							name.Level = _level;
							name.Name = _lineValue;
							name.Type = "aka";
							name.PreferedName = (individualRecord.Names.Count == 0);
							individualRecord.Names.Add(name);
						}
						break;
					case "SEX":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							switch (_lineValue)
							{
								case "M":
									individualRecord.Sex = GedcomSex.Male;
									break;
								case "F":
									individualRecord.Sex = GedcomSex.Female;
									break;
								// non standard
								case "B":
									individualRecord.Sex = GedcomSex.Both;
									break;
								// non standard
								case "N":
									individualRecord.Sex = GedcomSex.Neuter;
									break;
								// non standard
								case "U":
									individualRecord.Sex = GedcomSex.Undetermined;
									break;
							}
						}
						break;
					case "SUBM":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							individualRecord.SubmitterRecords.Add(_lineValue);
							_missingReferences.Add(_lineValue);
						}
						else
						{
							GedcomSubmitterRecord submitter = new GedcomSubmitterRecord();
							submitter.Level = 0; // new top level submitter, always 0
							submitter.ParsingLevel = _level;
							submitter.XRefID = Database.GenerateXref("SUBM");
							
							_ParseState.Records.Push(submitter);
							
							individualRecord.SubmitterRecords.Add(submitter.XRefID);
						}
						break;
					case "ALIA":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							individualRecord.Alia.Add(_lineValue);	
							_missingReferences.Add(_lineValue);
						}
						else if (_lineValueType == GedcomLineValueType.DataType)
						{
							// Family Tree Maker doing this?
							// ALIA is unsupported in gedcom 5.5 as a way of
							// adding multiple names, the spec
							// does say it should be a pointer to an individual
							// though, not another name.
							// spec allows multiple NAME though, so add one
							// with this name
							GedcomName name = new GedcomName();
							name.Database = _ParseState.Database;
							name.Level = _level;
							name.Name = _lineValue;
							name.Type = "aka";
							name.PreferedName = (individualRecord.Names.Count == 0);
							individualRecord.Names.Add(name);
						}
						break;
					case "ANCI":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							individualRecord.Anci.Add(_lineValue);	
							_missingReferences.Add(_lineValue);
						}
						break;
					case "DESI":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							individualRecord.Desi.Add(_lineValue);	
							_missingReferences.Add(_lineValue);
						}
						break;
					case "RFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualRecord.PermanentRecordFileNumber = _lineValue;	
						}
						break;
					case "AFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualRecord.AncestralFileNumber = _lineValue;	
						}
						break;
					case "REFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualRecord.UserReferenceNumber = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualRecord.AutomatedRecordID = _lineValue;
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(individualRecord);
						break;
					case "SOUR":
					    AddSourceCitation(individualRecord);
						break;
					case "OBJE":
						AddMultimediaRecord(individualRecord);
						break;
					case "BIRT":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.BIRT;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "CHR":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.CHR;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "DEAT":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.DEAT;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "BURI":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.BURI;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "CREM":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.CREM;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "ADOP":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.ADOP;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "BAPM":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.BAPM;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "BARM":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.BARM;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "BASM":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.BASM;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "BLES":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.BLES;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "CHRA":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.CHRA;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "CONF":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.CONF;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "FCOM":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.FCOM;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "ORDN":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.ORDN;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "NATU":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.NATU;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "EMIG":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.EMIG;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "IMMI":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.IMMI;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "CENS":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.CENS;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "PROB":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.PROB;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;	
					case "WILL":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.WILL;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "GRAD":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.GRAD;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "RETI":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.RETI;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "EVEN":
						
						// event
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.GenericEvent;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Events.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "CAST":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.CASTFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "DSCR":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.DSCRFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;	
					case "EDUC":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.EDUCFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;					
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "IDNO":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.IDNOFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "NATI":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.NATIFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "NCHI":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.NCHIFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "NMR":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.NMRFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "OCCU":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.OCCUFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "PROP":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.PROPFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "RELI":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.RELIFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "RESI":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.RESIFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;	
					case "SSN":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.SSNFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;		
					case "TITL":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.TITLFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;
						}
					
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
					case "FACT":
						
						// fact
						individualEvent = new GedcomIndividualEvent();
						individualEvent.EventType = GedcomEvent.GedcomEventType.GenericFact;
						individualEvent.Level = _level;
						individualEvent.IndiRecord = individualRecord;
					
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualEvent.EventName = _lineValue;						
						}
						if (!string.IsNullOrEmpty(customToGenericClassification))
						{
							individualEvent.Classification = customToGenericClassification;
						}
						individualRecord.Attributes.Add(individualEvent);
					
						_ParseState.Records.Push(individualEvent);
					
						break;
										
					// Not according to the spec, but Family Tree Maker sticks
					// an address under an individual so we will support reading it
					case "ADDR":
						if (individualRecord.Address == null)
						{
							individualRecord.Address = new GedcomAddress();
							individualRecord.Address.Database = Database;
						}
						
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							individualRecord.Address.AddressLine = _lineValue;
						}
						
						break;
					case "PHON":
						if (individualRecord.Address == null)
						{
							individualRecord.Address = new GedcomAddress();	
							individualRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(individualRecord.Address.Phone1))
							{
								individualRecord.Address.Phone1 = _lineValue;
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Phone2))
							{
								individualRecord.Address.Phone2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Phone3))
							{
								individualRecord.Address.Phone3 = _lineValue;
							}
							else
							{
								// TODO should never occur only 3 phone numbers are allowed	
							}
						}
						break;
					case "EMAIL":
						if (individualRecord.Address == null)
						{
							individualRecord.Address = new GedcomAddress();	
							individualRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(individualRecord.Address.Email1))
							{
								individualRecord.Address.Email1 = _lineValue;
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Email2))
							{
								individualRecord.Address.Email2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Email3))
							{
								individualRecord.Address.Email3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 emails are allowed	
							}
						}
						break;
					case "FAX":
						if (individualRecord.Address == null)
						{
							individualRecord.Address = new GedcomAddress();	
							individualRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(individualRecord.Address.Fax1))
							{
								individualRecord.Address.Fax1 = _lineValue;
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Fax2))
							{
								individualRecord.Address.Fax2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Fax3))
							{
								individualRecord.Address.Fax3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 fax numbers are allowed	
							}
						}
						break;
					case "WWW":
						if (individualRecord.Address == null)
						{
							individualRecord.Address = new GedcomAddress();	
							individualRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(individualRecord.Address.Www1))
							{
								individualRecord.Address.Www1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Www2))
							{
								individualRecord.Address.Www2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(individualRecord.Address.Www3))
							{
								individualRecord.Address.Www3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 urls are allowed	
							}
						}
						break;
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == _ParseState.PreviousLevel + 1)
			{
				string pTag = _ParseState.PreviousTag;
				
				if (pTag == "REFN" && _tag == "TYPE")
				{
					if (_lineValueType == GedcomLineValueType.DataType)
					{
						individualRecord.UserReferenceType = _lineValue;	
					}
				}
				else
				{
					AddressParse(individualRecord.Address, _tag, _lineValue, _lineValueType);	
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == _ParseState.PreviousLevel)
			{
				AddressParse(individualRecord.Address, _tag, _lineValue, _lineValueType);
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing individual (" + individualRecord.XRefID + ") node: " + _tag + "\t at level: " + _level);
				Console.WriteLine("Unknown state / tag parsing individual (" + individualRecord.XRefID + ") node: " + _tag + "\t at level: " + _level);
				Console.WriteLine("Previous tag: " + _ParseState.PreviousTag + "\tPrevious Level: " + _ParseState.PreviousLevel);
			}
		}
		
		private void ReadMultimediaRecord()
		{
		    GedcomMultimediaRecord multimediaRecord = _ParseState.Records.Peek() as GedcomMultimediaRecord;
            if (multimediaRecord == null)
                return;

		    if (_level == multimediaRecord.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "FORM":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomMultimediaFile file;
							if (multimediaRecord.Files.Count > 0)
							{
								file = multimediaRecord.Files[multimediaRecord.Files.Count - 1];
							}
							else
							{
								file = new GedcomMultimediaFile();
								file.Database = Database;
								multimediaRecord.Files.Add(file);
							}
							file.Format = _lineValue;
						}
						break;
					case "TITL":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							multimediaRecord.Title = _lineValue;
						}
						break;
						
				
					case "FILE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomMultimediaFile file = null;
							if (multimediaRecord.Files.Count > 0)
							{
								file = multimediaRecord.Files[multimediaRecord.Files.Count - 1];
								if (!string.IsNullOrEmpty(file.Filename))
								{
									file = null;
								}
							}
							if (file == null)
							{
								file = new GedcomMultimediaFile();
								file.Database = Database;
								multimediaRecord.Files.Add(file);
							}
							
							file.Filename = _lineValue;
						    file.Basepath = _gedcomPath;
						}
						break;
					case "REFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							multimediaRecord.UserReferenceNumber = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							multimediaRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(multimediaRecord);
						break;
					case "SOUR":
					    AddSourceCitation(multimediaRecord);				
						break;
				}
			}
			else if (_ParseState.PreviousTag != string.Empty)
			{
				if (_level == multimediaRecord.ParsingLevel + 2)
				{
					if (_ParseState.PreviousTag == "REFN" && _tag == "TYPE")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							multimediaRecord.UserReferenceType = _lineValue;	
						}
					}
					else if (_ParseState.PreviousTag == "FILE")
					{
						switch (_tag)
						{
							case "FORM":
								if (_lineValueType == GedcomLineValueType.DataType)
								{
									multimediaRecord.Files[multimediaRecord.Files.Count - 1].Format = _lineValue;	
								}
								break;
						}
					}
					else if (_ParseState.PreviousTag == "FORM")
					{
						if (_tag == "MEDI" &&
						    _lineValueType == GedcomLineValueType.DataType)
						{
							// FIXME: GedcomMultiMediaFile should use the enum?
							multimediaRecord.Files[multimediaRecord.Files.Count - 1].SourceMediaType  = _lineValue;
						}
					}
				}
				else if (_level == multimediaRecord.ParsingLevel + 3)
				{
					if (_ParseState.PreviousTag == "FILE" && _tag == "TYPE")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// FIXME: GedcomMultiMediaFile should use the enum?
							multimediaRecord.Files[multimediaRecord.Files.Count - 1].SourceMediaType = _lineValue;	
						}
					}
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing multimedia node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadNoteRecord()
	    {
	        GedcomNoteRecord noteRecord = _ParseState.Records.Peek() as GedcomNoteRecord;
            if (noteRecord == null)
                return;

	        if (_level == noteRecord.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "CONT":
						noteRecord.ParsedText.Append(Environment.NewLine);
						noteRecord.ParsedText.Append(_lineValue);
						break;
					case "CONC":
						noteRecord.ParsedText.Append(_lineValue);
						break;
					case "REFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							noteRecord.UserReferenceNumber = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							noteRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "SOUR":
					    AddSourceCitation(noteRecord);
						break;
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _ParseState.PreviousTag == "REFN" &&
			            _level == _ParseState.PreviousLevel + 1)
			{
				if (_tag == "TYPE")
				{
					if (_lineValueType == GedcomLineValueType.DataType)
					{
						noteRecord.UserReferenceType = _lineValue;	
					}
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing note node: " + _tag + "\t at level: " + _level);
			}
	    }

	    private void ReadRepositoryRecord()
		{
			if (_tag.StartsWith("_"))
			{
//				switch (_tag)
//				{
//					default:
						GedcomCustomRecord custom = new GedcomCustomRecord();
						custom.Level = _level;
						custom.XRefID = _xrefID;
						custom.Tag = _tag;
									
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							custom.Classification = _lineValue;
						}
						
						// FIXME: may want to use customs at some point
		
						_ParseState.Records.Push(custom);
//						break;
//				}
			}

            GedcomRepositoryRecord repositoryRecord = _ParseState.Records.Peek() as GedcomRepositoryRecord;
            if (repositoryRecord == null)
                return;

			if (_level == repositoryRecord.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "NAME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							repositoryRecord.Name = _lineValue;	
						}
						break;
					case "ADDR":
						if (repositoryRecord.Address == null)
						{
							repositoryRecord.Address = new GedcomAddress();
							repositoryRecord.Address.Database = Database;
						}
						
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							repositoryRecord.Address.AddressLine = _lineValue;	
						}
						
						break;
					case "PHON":
						if (repositoryRecord.Address == null)
						{
							repositoryRecord.Address = new GedcomAddress();	
							repositoryRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(repositoryRecord.Address.Phone1))
							{
								repositoryRecord.Address.Phone1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Phone2))
							{
								repositoryRecord.Address.Phone2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Phone3))
							{
								repositoryRecord.Address.Phone3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 phone numbers are allowed	
							}
						}
						break;
					case "EMAIL":
						if (repositoryRecord.Address == null)
						{
							repositoryRecord.Address = new GedcomAddress();	
							repositoryRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(repositoryRecord.Address.Email1))
							{
								repositoryRecord.Address.Email1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Email2))
							{
								repositoryRecord.Address.Email2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Email3))
							{
								repositoryRecord.Address.Email3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 emails are allowed	
							}
						}
						break;
					case "FAX":
						if (repositoryRecord.Address == null)
						{
							repositoryRecord.Address = new GedcomAddress();	
							repositoryRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(repositoryRecord.Address.Fax1))
							{
								repositoryRecord.Address.Fax1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Fax2))
							{
								repositoryRecord.Address.Fax2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Fax3))
							{
								repositoryRecord.Address.Fax3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 fax numbers are allowed	
							}
						}
						break;
					case "WWW":
						if (repositoryRecord.Address == null)
						{
							repositoryRecord.Address = new GedcomAddress();	
							repositoryRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(repositoryRecord.Address.Www1))
							{
								repositoryRecord.Address.Www1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Www2))
							{
								repositoryRecord.Address.Www2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(repositoryRecord.Address.Www3))
							{
								repositoryRecord.Address.Www3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 urls are allowed	
							}
						}
						break;
					case "REFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							repositoryRecord.UserReferenceNumber = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							repositoryRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(repositoryRecord);
						break;
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == repositoryRecord.Level + 2) //_ParseState.PreviousLevel + 2)
			{
				if (_ParseState.PreviousTag == "REFN" && _tag == "TYPE")
				{
					if (_lineValueType == GedcomLineValueType.DataType)
					{
						repositoryRecord.UserReferenceType = _lineValue;	
					}
				}
				else
				{
					 AddressParse(repositoryRecord.Address, _tag, _lineValue, _lineValueType);
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == _ParseState.PreviousLevel)
			{
				AddressParse(repositoryRecord.Address, _tag, _lineValue, _lineValueType);
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing note node: " + _tag + "\t at level: " + _level);
			}
		}
		
		private void ReadSourceRecord()
		{
		    GedcomSourceRecord sourceRecord = _ParseState.Records.Peek() as GedcomSourceRecord;
            if (sourceRecord == null)
                return;

		    if (_level == sourceRecord.ParsingLevel + 1)
			{

				// hack, at this level won't have CONT/CONC so end any building we
				// are doing
				if (sourceRecord.TitleText != null)
				{
					sourceRecord.Title = sourceRecord.TitleText.ToString();
					sourceRecord.TitleText = null;
				}
				else if (sourceRecord.OriginatorText != null)
				{
					sourceRecord.Originator = sourceRecord.OriginatorText.ToString();
					sourceRecord.OriginatorText = null;
				}
				else if (sourceRecord.PublicationText != null)
				{
					sourceRecord.PublicationFacts = sourceRecord.PublicationText.ToString();
					sourceRecord.PublicationText = null;
				}
				else if (sourceRecord.TextText != null)
				{
					sourceRecord.Text = sourceRecord.TextText.ToString();
					sourceRecord.TextText = null;
				}
			
				switch (_tag)
				{
					case "DATA":
						// info held in child nodes
						break;
					case "AUTH":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceRecord.OriginatorText = new StringBuilder(_lineValue);
						}
						break;
					case "TITL":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceRecord.TitleText = new StringBuilder(_lineValue);
						}
						break;
					case "ABBR":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceRecord.FiledBy = _lineValue;
						}
						break;
					case "PUBL":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceRecord.PublicationText = new StringBuilder(_lineValue);
						}
						break;
					case "TEXT":
                        // KBR empty text preventing building using CONC/CONT
						if (_lineValueType != GedcomLineValueType.PointerType)
						{
							int capacity = _lineValue.Length;
							if (!string.IsNullOrEmpty(sourceRecord.Text))
							{
								capacity += sourceRecord.Text.Length;
								capacity += Environment.NewLine.Length;
							}
							
							sourceRecord.TextText = new StringBuilder(capacity);
							
							if (string.IsNullOrEmpty(sourceRecord.Text))
							{
								sourceRecord.TextText.Append(_lineValue);
							}
							else
							{
								sourceRecord.TextText.Append(sourceRecord.Text);
								sourceRecord.TextText.Append(Environment.NewLine);
								sourceRecord.TextText.Append(_lineValue);	
							}
						}
						break;
					case "REPO":
						GedcomRepositoryCitation citation = new GedcomRepositoryCitation();
						citation.Level = _level;
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							citation.Repository = _lineValue;
							_missingReferences.Add(_lineValue);
						}
						sourceRecord.RepositoryCitations.Add(citation);
					
						_ParseState.Records.Push(citation);
						break;
					case "REFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceRecord.UserReferenceNumber = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(sourceRecord);
						break;
					case "OBJE":
						AddMultimediaRecord(sourceRecord);
						break;

				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == sourceRecord.Level + 2) // _ParseState.PreviousLevel + 2)
			{
				if (_ParseState.PreviousTag == "REFN" && _tag == "TYPE")
				{
					if (_lineValueType == GedcomLineValueType.DataType)
					{
						sourceRecord.UserReferenceType = _lineValue;	
					}
				}
				else if (sourceRecord.OriginatorText != null) // (_ParseState.PreviousTag == "AUTH")
				{
					switch (_tag)
					{
						case "CONT":
							sourceRecord.OriginatorText.Append(Environment.NewLine);
							sourceRecord.OriginatorText.Append(_lineValue);
							break;
						case "CONC":
							sourceRecord.OriginatorText.Append(_lineValue);
							break;
					}
				}
				else if (sourceRecord.TitleText != null) // (_ParseState.PreviousTag == "TITL")
				{
					switch (_tag)
					{
						case "CONT":
							sourceRecord.TitleText.Append(Environment.NewLine);
							sourceRecord.TitleText.Append(_lineValue);
							break;
						case "CONC":
							sourceRecord.TitleText.Append(_lineValue);
							break;
					}
				}
				else if (sourceRecord.PublicationText != null) // (_ParseState.PreviousTag == "PUBL")
				{
					switch (_tag)
					{
						case "CONT":
							sourceRecord.PublicationText.Append(Environment.NewLine);
							sourceRecord.PublicationText.Append(_lineValue);
							break;
						case "CONC":
							sourceRecord.PublicationText.Append(_lineValue);
							break;
					}
				}
				else if (sourceRecord.TextText != null) //(_ParseState.PreviousTag == "TEXT")
				{
					switch (_tag)
					{
						case "CONT":
							sourceRecord.TextText.Append(Environment.NewLine);
							sourceRecord.TextText.Append(_lineValue);
							break;
						case "CONC":
							sourceRecord.TextText.Append(_lineValue);
							break;
					}
				}
				else //if (_ParseState.PreviousTag == "DATA")
				{
					switch (_tag)
					{
						case "AGNC":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								sourceRecord.Agency = _lineValue;	
							}
							break;
						case "EVEN":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								GedcomRecordedEvent recordedEvent = new GedcomRecordedEvent();
								
								sourceRecord.EventsRecorded.Add(recordedEvent);
								
								string[] events = _lineValue.Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries);
								foreach (string e in events)
								{
									string ev = e.Trim();
									GedcomEvent.GedcomEventType eventType;
									
									if (ev == "EVEN")
									{
										eventType = GedcomEvent.GedcomEventType.GenericEvent;
										recordedEvent.Types.Add(eventType);
									}
									else if (ev == "FACT")
									{
										eventType = GedcomEvent.GedcomEventType.GenericFact;
										recordedEvent.Types.Add(eventType);
									}
									else
									{
                                        GedcomEvent.GedcomEventType tmp;
                                        if (Enum.TryParse(ev, true, out tmp))
                                            recordedEvent.Types.Add(tmp);
                                        else
                                        {
                                            if (Enum.TryParse(ev+"Fact", true, out tmp))
                                                recordedEvent.Types.Add(tmp);
                                            else
                                            {
                                                // FIXME: shouldn't lose data like this
                                            }
                                        }
									}									
								}
							}
							break;
						case "NOTE":
						    string xref = AddNoteRecord(sourceRecord);
						    // belongs in data records, not top level record notes
						    sourceRecord.Notes.Remove(xref);
						    sourceRecord.DataNotes.Add(xref);
							break;
					}
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == sourceRecord.Level + 3) //_ParseState.PreviousLevel + 3)
			{
//				if (_ParseState.PreviousTag == "EVEN")
//				{
					GedcomRecordedEvent recordedEvent = sourceRecord.EventsRecorded[sourceRecord.EventsRecorded.Count - 1];
					switch (_tag)
					{
						case "DATE":
							GedcomDate date = new GedcomDate(Database);
							date.Level = _level;
							_ParseState.Records.Push(date);
							recordedEvent.Date = date;
							_level ++;
							ReadDateRecord();
							_level --;
							_ParseState.Records.Pop();
							break;
						case "PLAC":
							GedcomPlace place = new GedcomPlace();
							place.Level = _level;
						
							recordedEvent.Place = place; 
														
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								place.Name = Database.PlaceNameCollection[_lineValue];
							}
							else
							{
								// invalid, provide a name anyway
								place.Name = "Unknown";
								Debug.WriteLine("invalid place node, no name at level: " + _level);
							}
							_ParseState.Records.Push(place);
							break;
					}
//				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing note node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadSubmitterRecord()
		{
			if (_tag.StartsWith("_"))
			{
//				switch (_tag)
//				{
//					default:
						GedcomCustomRecord custom = new GedcomCustomRecord();
						custom.Level = _level;
						custom.XRefID = _xrefID;
						custom.Tag = _tag;
									
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							custom.Classification = _lineValue;
						}
						
						// FIXME: may want to use customs at some point
		
						_ParseState.Records.Push(custom);
//						break;
//				}
			}

            GedcomSubmitterRecord submitterRecord = _ParseState.Records.Peek() as GedcomSubmitterRecord;
            if (submitterRecord == null)
                return;

			if (_level == submitterRecord.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "NAME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submitterRecord.Name = _lineValue;
						}
						break;
					case "ADDR":
						if (submitterRecord.Address == null)
						{
							submitterRecord.Address = new GedcomAddress();
							submitterRecord.Address.Database = Database;
						}
						
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submitterRecord.Address.AddressLine = _lineValue;	
						}
						
						break;
					case "PHON":
						if (submitterRecord.Address == null)
						{
							submitterRecord.Address = new GedcomAddress();	
							submitterRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(submitterRecord.Address.Phone1))
							{
								submitterRecord.Address.Phone1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Phone2))
							{
								submitterRecord.Address.Phone2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Phone3))
							{
								submitterRecord.Address.Phone3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 phone numbers are allowed	
							}
						}
						break;
					case "EMAIL":
						if (submitterRecord.Address == null)
						{
							submitterRecord.Address = new GedcomAddress();	
							submitterRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(submitterRecord.Address.Email1))
							{
								submitterRecord.Address.Email1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Email2))
							{
								submitterRecord.Address.Email2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Email3))
							{
								submitterRecord.Address.Email3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 emails are allowed	
							}
						}
						break;
					case "FAX":
						if (submitterRecord.Address == null)
						{
							submitterRecord.Address = new GedcomAddress();	
							submitterRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(submitterRecord.Address.Fax1))
							{
								submitterRecord.Address.Fax1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Fax2))
							{
								submitterRecord.Address.Fax2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Fax3))
							{
								submitterRecord.Address.Fax3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 fax numbers are allowed	
							}
						}
						break;
					case "WWW":
						if (submitterRecord.Address == null)
						{
							submitterRecord.Address = new GedcomAddress();	
							submitterRecord.Address.Database = Database;
						}
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (string.IsNullOrEmpty(submitterRecord.Address.Www1))
							{
								submitterRecord.Address.Www1 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Www2))
							{
								submitterRecord.Address.Www2 = _lineValue;	
							}
							else if (string.IsNullOrEmpty(submitterRecord.Address.Www3))
							{
								submitterRecord.Address.Www3 = _lineValue;	
							}
							else
							{
								// TODO should never occur only 3 urls are allowed	
							}
						}
						break;
					case "OBJE":
						AddMultimediaRecord(submitterRecord);
						break;
					case "LANG":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// only 3 lang are allowed
							for (int i = 0; i < 3; i ++)
							{
								if (string.IsNullOrEmpty(submitterRecord.LanguagePreferences[i]))
								{
									submitterRecord.LanguagePreferences[i] = _lineValue;
								    break; // Don't repeat for each preference!
								}
							}
						}
						break;
					case "RFN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submitterRecord.RegisteredRFN = _lineValue;
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submitterRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(submitterRecord);
						break;
				}
			}
			else if ( (!string.IsNullOrEmpty(_ParseState.PreviousTag)) &&
			            _level == submitterRecord.Level + 2) //_ParseState.PreviousLevel + 2)
			{
				AddressParse(submitterRecord.Address, _tag, _lineValue, _lineValueType);
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing submitter node: " + _tag + "\t at level: " + _level);
			}
		}
		
		private void ReadSubmissionRecord()
		{
		    GedcomSubmissionRecord submissionRecord = _ParseState.Records.Peek() as GedcomSubmissionRecord;
            if (submissionRecord == null)
                return;

		    if (_level == submissionRecord.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "SUBM":
						if (_lineValueType == GedcomLineValueType.PointerType)
						{
							submissionRecord.Submitter = _lineValue;
							_missingReferences.Add(_lineValue);
						}
						else
						{
							GedcomSubmitterRecord submitter = new GedcomSubmitterRecord();
							submitter.Level = 0; // new top level submitter, always 0;
							submitter.ParsingLevel = _level;
							submitter.XRefID = Database.GenerateXref("SUBM");
							
							_ParseState.Records.Push(submitter);
							
							submissionRecord.Submitter = submitter.XRefID;
                        }
						
						break;
					case "FAMF":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submissionRecord.FamilyFile = _lineValue;	
						}
						break;
					case "TEMP":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submissionRecord.TempleCode = _lineValue;	
						}
						break;
					case "ANCE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							int num;
							if (int.TryParse(_lineValue, out num))
							{
								submissionRecord.GenerationsOfAncestors = num;
							}
						}
						break;
					case "DESC":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							int num;
							if (int.TryParse(_lineValue, out num))
							{
								submissionRecord.GenerationsOfDecendants = num;
							}
						}
						break;
					case "ORDI":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submissionRecord.OrdinanceProcessFlag = (string.Compare(_lineValue, "YES", true) == 0); 	
						}
						break;
					case "RIN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							submissionRecord.AutomatedRecordID = _lineValue;	
						}
						break;
					case "CHAN":
						GedcomChangeDate date = new GedcomChangeDate(Database);
						date.Level = _level;
						_ParseState.Records.Push(date);
						break;
					case "NOTE":
					    AddNoteRecord(submissionRecord);
						break;
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing submission node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadEventRecord()
		{
			if (_tag.StartsWith("_"))
			{
                //switch (_tag)
                //{
                //    default:
						GedcomCustomRecord custom = new GedcomCustomRecord();
						custom.Level = _level;
						custom.XRefID = _xrefID;
						custom.Tag = _tag;
									
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							custom.Classification = _lineValue;
						}
						
						// FIXME: may want to use customs at some point
		
						_ParseState.Records.Push(custom);
                //        break;
                //}
			}

            GedcomEvent eventRecord = _ParseState.Records.Peek() as GedcomEvent;
            if (eventRecord == null)
                return;

            bool done = false;
			switch (eventRecord.RecordType)
			{
				case GedcomRecordType.FamilyEvent:
					GedcomFamilyEvent famEvent = eventRecord as GedcomFamilyEvent;
					if (_level == eventRecord.ParsingLevel + 2 && _tag == "AGE")
					{
						if (_ParseState.PreviousTag == "HUSB")
						{
							GedcomAge age = GedcomAge.Parse(_lineValue, Database); 
							famEvent.HusbandAge = age;
							done = true;					
						}
						else if (_ParseState.PreviousTag == "WIFE")
						{
							GedcomAge age = GedcomAge.Parse(_lineValue, Database);
							famEvent.WifeAge = age;
							done = true;	
						}
					}
					else if (_level == eventRecord.ParsingLevel + 1)
					{
						done = (_tag == "HUSB" || _tag == "WIFE");	
					}
					break;
				case GedcomRecordType.IndividualEvent:
					GedcomIndividualEvent individualEvent = eventRecord as GedcomIndividualEvent;
					if (_level == eventRecord.ParsingLevel + 1)
					{
						if (_tag == "AGE")
						{
							GedcomAge age = GedcomAge.Parse(_lineValue, Database);
							individualEvent.Age = age;
							done = true;	
						}
						else if (_tag == "FAMC" && 
					               (eventRecord.EventType == GedcomEvent.GedcomEventType.BIRT ||
                                     eventRecord.EventType == GedcomEvent.GedcomEventType.CHR ||
                                     eventRecord.EventType == GedcomEvent.GedcomEventType.ADOP))
	                    {
	                    	if (_lineValueType == GedcomLineValueType.PointerType)
	                    	{
	                    		individualEvent.Famc = _lineValue;
	                    		_missingReferences.Add(_lineValue);
	                    	}
	                    	done = true;
	                    }
	                    else if (_tag == "CONT" && 
	                             eventRecord.EventType == GedcomEvent.GedcomEventType.DSCRFact)
	                    {
	                    	eventRecord.Classification += Environment.NewLine;
	                    	eventRecord.Classification += _lineValue;
	                    }
	                    else if (_tag == "CONC" && 
	                             eventRecord.EventType == GedcomEvent.GedcomEventType.DSCRFact)
	                    {
	                    	//eventRecord.Description += " ";
	                    	eventRecord.Classification += _lineValue;
	                    }
					}
					else if (_level == eventRecord.ParsingLevel + 2)
					{
						if (_tag == "ADOP" && 
						    eventRecord.EventType == GedcomEvent.GedcomEventType.ADOP)
						{
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								if (_lineValue == "HUSB")
								{
									individualEvent.AdoptedBy = GedcomAdoptionType.Husband;	
								}
								else if (_lineValue == "WIFE")
								{
									individualEvent.AdoptedBy = GedcomAdoptionType.Wife;	
								}
								else if (_lineValue == "BOTH")
								{
									individualEvent.AdoptedBy = GedcomAdoptionType.HusbandAndWife;	
								}
							}
							done = true;
						}
					}
					break;
			}
			
			if (!done)
			{
				if (_level == eventRecord.ParsingLevel + 1)
				{
					switch (_tag)
					{
						case "TYPE":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								// if the event is generic, but the type
								// can be mapped to an actual event type
								// convert it.
								bool convertedEventType = false;
								if ((eventRecord.EventType == GedcomEvent.GedcomEventType.GenericEvent ||
								     eventRecord.EventType == GedcomEvent.GedcomEventType.GenericFact)
								    && string.IsNullOrEmpty(eventRecord.EventName))
								{
									GedcomEvent.GedcomEventType type = GedcomEvent.ReadableToType(_lineValue);
									if (type != GedcomEvent.GedcomEventType.GenericEvent)
									{
										eventRecord.EventType = type;
										convertedEventType = true;
									}
								}
								
								if (!convertedEventType)
								{
									// in TGC551LF  (torture test gedcom file) TYPE is set
									// to the same as the event tag name in some instances
									// this is stupid, so if _lineValue is the same
									// as the event tag, don't set it.
									string eventTag = _ParseState.ParentTag(_level);
									if (_lineValue != eventTag)
									{
										eventRecord.Classification = _lineValue;	
									}
								}
							}
							break;
						case "DATE":
							GedcomDate date = new GedcomDate(Database);
							date.Database = Database;
							date.Level = _level;
							_ParseState.Records.Push(date);
							eventRecord.Date = date;
							_level ++;
							ReadDateRecord();
							_level --;
							_ParseState.Records.Pop();
							break;
						case "PLAC":
							GedcomPlace place = new GedcomPlace();
							place.Database = Database;
							place.Level = _level;
						
							eventRecord.Place = place; 
														
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								place.Name = _lineValue.Trim();	// KBR trim excess whitespace
							}
							else
							{
								// invalid, provide a name anyway
								place.Name = string.Empty; //"Unknown";
								Debug.WriteLine("invalid place node, no name at level: " + _level);
							}
							_ParseState.Records.Push(place);
							break;
						case "ADDR":
							if (eventRecord.Address == null)
							{
								eventRecord.Address = new GedcomAddress();
								eventRecord.Address.Database = Database;
							}
							
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								eventRecord.Address.AddressLine = _lineValue;	
							}
							
							break;
						case "PHON":
							if (eventRecord.Address == null)
							{
								eventRecord.Address = new GedcomAddress();	
								eventRecord.Address.Database = Database;
							}
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								if (string.IsNullOrEmpty(eventRecord.Address.Phone1))
								{
									eventRecord.Address.Phone1 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Phone2))
								{
									eventRecord.Address.Phone2 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Phone3))
								{
									eventRecord.Address.Phone3 = _lineValue;	
								}
								else
								{
									// TODO should never occur only 3 phone numbers are allowed	
								}
							}
							break;
						case "EMAIL":
							if (eventRecord.Address == null)
							{
								eventRecord.Address = new GedcomAddress();	
								eventRecord.Address.Database = Database;
							}
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								if (string.IsNullOrEmpty(eventRecord.Address.Email1))
								{
									eventRecord.Address.Email1 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Email2))
								{
									eventRecord.Address.Email2 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Email3))
								{
									eventRecord.Address.Email3 = _lineValue;	
								}
								else
								{
									// TODO should never occur only 3 emails are allowed	
								}
							}
							break;
						case "FAX":
							if (eventRecord.Address == null)
							{
								eventRecord.Address = new GedcomAddress();	
								eventRecord.Address.Database = Database;
							}
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								if (string.IsNullOrEmpty(eventRecord.Address.Fax1))
								{
									eventRecord.Address.Fax1 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Fax2))
								{
									eventRecord.Address.Fax2 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Fax3))
								{
									eventRecord.Address.Fax3 = _lineValue;	
								}
								else
								{
									// TODO should never occur only 3 fax numbers are allowed	
								}
							}
							break;
						case "WWW":
							if (eventRecord.Address == null)
							{
								eventRecord.Address = new GedcomAddress();
								eventRecord.Address.Database = Database;
							}
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								if (string.IsNullOrEmpty(eventRecord.Address.Www1))
								{
									eventRecord.Address.Www1 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Www2))
								{
									eventRecord.Address.Www2 = _lineValue;	
								}
								else if (string.IsNullOrEmpty(eventRecord.Address.Www3))
								{
									eventRecord.Address.Www3 = _lineValue;	
								}
								else
								{
									// TODO should never occur only 3 urls are allowed	
								}
							}
							break;
						case "AGNC":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								eventRecord.ResponsibleAgency = _lineValue;	
							}
							break;
						case "RELI":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								eventRecord.ReligiousAffiliation = _lineValue;	
							}
							break;
						case "CAUS":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								eventRecord.Cause = _lineValue;	
							}
							break;
						case "RESN":
							// restriction notice
							if (_lineValueType == GedcomLineValueType.DataType)
							{
							    GedcomRestrictionNotice tmp;
                                if (Enum.TryParse(_lineValue, true, out tmp))
                                    eventRecord.RestrictionNotice = tmp;
                                else
								{
									Debug.WriteLine("Invalid restriction type: " + _lineValue);
									
									// default to confidential to protect privacy
									eventRecord.RestrictionNotice = GedcomRestrictionNotice.Confidential;
								}
							}
							break;
						case "NOTE":
						    AddNoteRecord(eventRecord);
							break;
						case "SOUR":
						    AddSourceCitation(eventRecord);					
							break;
						case "OBJE":
							AddMultimediaRecord(eventRecord);
							break;
						case "QUAY":
							if (_lineValueType == GedcomLineValueType.DataType)
							{
								int certainty = Convert.ToInt32(_lineValue);
								if ((certainty > (int)GedcomCertainty.Primary) ||
								    (certainty < (int)GedcomCertainty.Unreliable))
								{
									certainty = (int)GedcomCertainty.Unreliable;           	
								}
								eventRecord.Certainty = (GedcomCertainty)certainty;
							}
							break;
					}
				}
				else if (_ParseState.PreviousTag != string.Empty && _level == eventRecord.ParsingLevel + 2)
				{
					AddressParse(eventRecord.Address, _tag, _lineValue, _lineValueType);	
				}
			}
		}
		
		private void ReadPlaceRecord()
		{
		    GedcomPlace place = _ParseState.Records.Peek() as GedcomPlace;
            if (place == null)
                return;

		    if (_level == place.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "FORM":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							place.Form = _lineValue;	
						}
						break;
					case "FONE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = new GedcomVariation();
							variation.Database = Database;
							variation.Value = _lineValue;
							
							place.PhoneticVariations.Add(variation);
						}
						break;
					case "ROMN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = new GedcomVariation();
							variation.Database = Database;
							variation.Value = _lineValue;
							
							place.RomanizedVariations.Add(variation);
						}
						break;
					case "MAP":
						// map, longitude / latitude stored as child nodes
						break;
					case "NOTE":
					    AddNoteRecord(place);
						break;					
				}
			}
			else if (_ParseState.PreviousTag != string.Empty && _level == place.ParsingLevel + 2)
			{
				if (_tag == "TYPE")
				{
					if (_ParseState.PreviousTag == "FONE")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = place.PhoneticVariations[place.PhoneticVariations.Count - 1];
							variation.VariationType = _lineValue;
						}
					}
					else if (_ParseState.PreviousTag == "ROMN")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = place.RomanizedVariations[place.RomanizedVariations.Count - 1];
							variation.VariationType = _lineValue;
						}
					}
				}
				else if (_ParseState.PreviousTag == "MAP")
				{
					if (_tag == "LATI")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							place.Latitude = _lineValue;	
						}
					}
					else if (_tag == "LONG")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							place.Longitude = _lineValue;	
						}
					}
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing place node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadSourceCitationRecord()
		{
		    GedcomSourceCitation sourceCitation = _ParseState.Records.Peek() as GedcomSourceCitation;
            if (sourceCitation == null)
                return;
			
			GedcomSourceRecord sourceRecord = null;
			
			if (_ParseState.Database.Contains(sourceCitation.Source))
			{
				sourceRecord = _ParseState.Database[sourceCitation.Source] as GedcomSourceRecord;	
			}
						
			if (_level == sourceCitation.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "PAGE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceCitation.Page = _lineValue;	
						}
						break;
					case "CONT":
						if (sourceRecord != null)
						{
							sourceRecord.Title += Environment.NewLine;
							sourceRecord.Title += _lineValue;
						}
						break;
					case "CONC":
						if (sourceRecord != null)
						{
							sourceRecord.Title += _lineValue;
						}
						break;
					case "TEXT":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (sourceCitation.ParsedText == null)
							{
								int capacity = _lineValue.Length;
								if (!string.IsNullOrEmpty(sourceCitation.Text))
								{
									capacity += sourceCitation.Text.Length;
									capacity += Environment.NewLine.Length;
								}
								sourceCitation.ParsedText = new StringBuilder(capacity);
							}
							
							if (!string.IsNullOrEmpty(sourceCitation.Text))
							{
								sourceCitation.ParsedText.Append(Environment.NewLine);
							}
							sourceCitation.ParsedText.Append(_lineValue);
						}
						break;
					case "DATA":
						// data tag, just contains child tags
						break;
					case "EVEN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							sourceCitation.EventType = _lineValue;	
						}
						break;
					case "OBJE":
						AddMultimediaRecord(sourceCitation);
						break;
					case "NOTE":
					    AddNoteRecord(sourceCitation);
						break;
					case "QUAY":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							int certainty = Convert.ToInt32(_lineValue);
							if ((certainty > (int)GedcomCertainty.Primary) ||
							    (certainty < (int)GedcomCertainty.Unreliable))
							{
								certainty = (int)GedcomCertainty.Unreliable;           	
							}
							sourceCitation.Certainty = (GedcomCertainty)certainty;
						}
						break;
				}
			}
			else if (_ParseState.PreviousTag != string.Empty && _level == sourceCitation.ParsingLevel + 2)
			{
				if (_ParseState.PreviousTag == "EVEN" && _tag == "ROLE") 
				{
					if (_lineValueType == GedcomLineValueType.DataType)
					{
						sourceCitation.Role = _lineValue;	
					}
				}
				else  //if (_ParseState.PreviousTag == "DATA")
				{
					if (_tag == "DATE") 
					{
					    var date = new GedcomDate(Database) {Level = _level};

					    _ParseState.Records.Push(date);
						sourceCitation.Date = date;
						_level ++;
						ReadDateRecord();
						_level --;
						_ParseState.Records.Pop();
					}
					else if (_tag == "TEXT")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							if (sourceCitation.ParsedText == null)
							{
								int capacity = _lineValue.Length;
								if (!string.IsNullOrEmpty(sourceCitation.Text))
								{
									capacity += sourceCitation.Text.Length;
									capacity += Environment.NewLine.Length;
								}
								sourceCitation.ParsedText = new StringBuilder(capacity);
							}
							
							if (!string.IsNullOrEmpty(sourceCitation.Text))
							{
								sourceCitation.ParsedText.Append(Environment.NewLine);
							}
							sourceCitation.ParsedText.Append(_lineValue);
						}
					}
				//}
				//else if (_ParseState.PreviousTag == "TEXT")
				//{
					else if (_tag == "CONC")
					{
						if (sourceCitation.ParsedText == null)
						{
							sourceCitation.ParsedText = new StringBuilder(_lineValue.Length);
						}
						sourceCitation.ParsedText.Append(_lineValue);
					}
					else if (_tag == "CONT")
					{
						if (sourceCitation.ParsedText == null)
						{
							int capacity = _lineValue.Length + Environment.NewLine.Length;
							sourceCitation.ParsedText = new StringBuilder(capacity);
						}
						sourceCitation.ParsedText.Append(Environment.NewLine);
						sourceCitation.ParsedText.Append(_lineValue);
					}
				}
			}
			else if (_ParseState.PreviousTag != string.Empty && _level == sourceCitation.ParsingLevel + 3)
			{
				if (_ParseState.PreviousTag == "TEXT" || _ParseState.PreviousTag == "CONC" || _ParseState.PreviousTag == "CONT")
				{
					if (_tag == "CONC")
					{
						if (sourceCitation.ParsedText == null)
						{
							sourceCitation.ParsedText = new StringBuilder(_lineValue.Length);
						}
						sourceCitation.ParsedText.Append(_lineValue);
					}
					else if (_tag == "CONT")
					{
						if (sourceCitation.ParsedText == null)
						{
							int capacity = _lineValue.Length + Environment.NewLine.Length;
							sourceCitation.ParsedText = new StringBuilder(capacity);
						}
						sourceCitation.ParsedText.Append(Environment.NewLine);
						sourceCitation.ParsedText.Append(_lineValue);
					}
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing source citation node: " + _tag + "\t at level: " + _level);
			}
		}
		
		private void ReadFamilyLinkRecord()
		{
		    GedcomFamilyLink childOf = _ParseState.Records.Peek() as GedcomFamilyLink;
            if (childOf == null)
                return;

		    if (_level == childOf.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "PEDI":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
						    PedegreeLinkageType tmp;
                            if (Enum.TryParse(_lineValue, true, out tmp))
                                childOf.Pedigree = tmp;
                            else
                            {
                                Debug.WriteLine("Invalid pedegree linkage type: " + _lineValue);
                                childOf.Pedigree = PedegreeLinkageType.Unknown;
                            }
						}
						break;
					case "STAT":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
						    ChildLinkageStatus tmp;
                            if (Enum.TryParse(_lineValue, true, out tmp))
                                childOf.Status = tmp;
                            else
                            {
                                Debug.WriteLine("Invalid child linkage status type: " + _lineValue);

                                childOf.Status = ChildLinkageStatus.Unknown;
                            }
						}
						break;
					case "NOTE":
					    AddNoteRecord(childOf);
						break;
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing family link node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadAssociationRecord()
	    {
	        GedcomAssociation association = _ParseState.Records.Peek() as GedcomAssociation;
            if (association == null)
                return;

	        if (_level == association.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "RELA":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							association.Description = _lineValue;	
						}
						break;
					case "NOTE":
					    AddNoteRecord(association);
						break;
					case "SOUR":
					    AddSourceCitation(association);	
						break;
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing association node: " + _tag + "\t at level: " + _level);
			}
	    }

	    private void ReadNameRecord()
		{
		    GedcomName name = _ParseState.Records.Peek() as GedcomName;
            if (name == null)
                return;

		    if (_level == name.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "TYPE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							name.Type = _lineValue;	
						}
						break;
					case "FONE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = new GedcomVariation();
							variation.Database = Database;
							variation.Value = _lineValue;
							
							name.PhoneticVariations.Add(variation);
						}
						break;
					case "ROMN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = new GedcomVariation();
							variation.Database = Database;
							variation.Value = _lineValue;
							
							name.RomanizedVariations.Add(variation);
						}
						break;
					case "NPFX":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// Prefix from NAME has priority
							if (string.IsNullOrEmpty(name.Prefix))
							{
								name.Prefix = _lineValue;
							}
						}
						break;
					case "GIVN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// Given from NAME has priority
							if (string.IsNullOrEmpty(name.Given))
							{
								name.Given = _lineValue;
							}
						}
						break;
					case "NICK":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							name.Nick = _lineValue;
						}
						break;
					case "SPFX":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// surname prefix from NAME has priority
							if (string.IsNullOrEmpty(name.SurnamePrefix))
							{
								name.SurnamePrefix = _lineValue;
							}
						}
						break;
					case "SURN":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// surname from NAME has priority
							if (string.IsNullOrEmpty(name.Given))
							{
								name.Surname = _lineValue;
							}
						}
						break;
					case "NSFX":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							// suffix from NAME has priority
							if (string.IsNullOrEmpty(name.Suffix))
							{
								name.Suffix = _lineValue;
							}
						}
						break;
					case "NOTE":
					    AddNoteRecord(name);
						break;
					case "SOUR":
					    AddSourceCitation(name);	
						break;
				}
			}
			else if (_ParseState.PreviousTag != string.Empty && _level == name.ParsingLevel + 2)
			{
				if (_tag == "TYPE")
				{
					if (_ParseState.PreviousTag == "FONE")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = name.PhoneticVariations[name.PhoneticVariations.Count - 1];
							variation.VariationType = _lineValue;
						}
					}
					else if (_ParseState.PreviousTag == "ROMN")
					{
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							GedcomVariation variation = name.RomanizedVariations[name.RomanizedVariations.Count - 1];
							variation.VariationType = _lineValue;
						}
					}
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing name node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadDateRecord()
		{
		    var date = _ParseState.Records.Peek() as GedcomDate;
            if (date == null)
                return;

		    if (_level == date.ParsingLevel + 1)
			{
				switch (_tag)
				{
					// Yes this does seem odd a DATE when we are already parsing
					//  a GedcomDateRecord.  The reason for this is that
					// we treat a CHAN as a GedcomDate as that is all it really is
					// and it contains the DATE as a child tag, so at level + 1
					case "DATE":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							DateParse(date,_lineValue);
						}
						break;
					// Again, CHAN can have notes	
					case "NOTE":
					    AddNoteRecord(date);
						break;
					// for a normal DATE +1 is correct, for a CHAN, +2
					case "TIME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							date.Time = _lineValue;	
						}
						break;
					// sources aren't allowed on change dates, however family tree maker
					// is known to put them in, we won't bother differentiating
					// dates and change dates so we will just allow on either
					case "SOUR":
						AddSourceCitation(date);
						break;
				}
			}
			else if (_level == date.ParsingLevel + 2)
			{
				switch (_tag)
				{
					// Time for a CHAN
					case "TIME":
						if (_lineValueType == GedcomLineValueType.DataType)
						{
							date.Time = _lineValue;	
						}
						break;
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing date node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void ReadRepositoryCitation()
		{
		    var citation = _ParseState.Records.Peek() as GedcomRepositoryCitation;
            if (citation == null)
                return;

		    if (_level == citation.ParsingLevel + 1)
			{
				switch (_tag)
				{
					case "NOTE":
					    AddNoteRecord(citation);
						break;
					case "CALN":					
						citation.CallNumbers.Add(_lineValue);
						citation.MediaTypes.Add(SourceMediaType.None);
						break;
				}
			}
			else if (_ParseState.PreviousTag == "CALN" && 
			         _level == citation.ParsingLevel + 2)
			{
				if (_tag == "MEDI" && 
				    _lineValueType == GedcomLineValueType.DataType)
				{
					SourceMediaType sourceMediaType;
					string val = _lineValue.Replace(" ", "_");
                    if (!Enum.TryParse(val, true, out sourceMediaType))
                    {
                        sourceMediaType = SourceMediaType.Other;
                    }

					// TGC551LF.GED has an invalid MEDI value
					// "Book (or other description of this source)"
					//
					// Spec says:
					//
					// [ audio | book | card | electronic | fiche | film | magazine |
 					// manuscript | map | newspaper | photo | tombstone | video ]
					// A code, selected from one of the media classifications choices above,
					// that indicates the type of material in which the referenced source is stored.
					//
					// We support other types as well as defined by SourceMediaType,
					// and other types should be added as needed.  This field is NOT for a
					// description.
					// Set to other and set other field for this media type to hold the
					// value entered.
                    if (sourceMediaType == SourceMediaType.Other)
                    {
                        citation.OtherMediaTypes.Add(_lineValue);
                    }

					citation.MediaTypes[citation.MediaTypes.Count - 1] = sourceMediaType; 	
				}
			}
			else
			{
				// shouldn't be here
				Debug.WriteLine("Unknown state / tag parsing repo node: " + _tag + "\t at level: " + _level);
			}
		}

	    private void AddSourceCitation(GedcomRecord record)
		{
			var sourceCitation = new GedcomSourceCitation();
			sourceCitation.Level = _level;
			sourceCitation.Database = _ParseState.Database;
		
			if (_lineValueType == GedcomLineValueType.PointerType)
			{
				sourceCitation.Source = _lineValue;	
				_missingReferences.Add(_lineValue);
			}
			else
			{
				var source = new GedcomSourceRecord();
				source.Level = 0; // new top level source, always 0
				source.ParsingLevel = _level;
				source.XRefID = Database.GenerateXref("SOUR");
				
				if (_lineValue != string.Empty)
				{
					source.Title = _lineValue;	
				}
				
				sourceCitation.Source = source.XRefID;
				
				_ParseState.Database.Add(source.XRefID,source);
			}
			
			record.Sources.Add(sourceCitation);
			_ParseState.Records.Push(sourceCitation);
			
			_sourceCitations.Add(sourceCitation);
		}

		private string AddNoteRecord(GedcomRecord record)
		{
			string xref = string.Empty;
			
			if (_lineValueType == GedcomLineValueType.PointerType)
			{
				if (!_removedNotes.Contains(_lineValue))
				{
					record.Notes.Add(_lineValue);
					xref = _lineValue;
					_missingReferences.Add(_lineValue);
				}
			}
			else
			{
				var note = new GedcomNoteRecord();
				note.Level = 0; // new top level note, always 0 (not true, 1 in header, fixed up later)
				note.ParsingLevel = _level;
				note.XRefID = Database.GenerateXref("NOTE");
				
				if (_lineValue != string.Empty)
				{
					note.ParsedText.Append(_lineValue);
				}
				
				_ParseState.Records.Push(note);
				
				record.Notes.Add(note.XRefID);
				xref = note.XRefID;
			}	
			
			return xref;
		}

		private void AddMultimediaRecord(GedcomRecord record)
		{
			if (_lineValueType == GedcomLineValueType.PointerType)
			{
				record.Multimedia.Add(_lineValue);	
				_missingReferences.Add(_lineValue);
			}
			else
			{
				var multimedia = new GedcomMultimediaRecord();
				multimedia.Level = 0; // new top level multimedia, always 0
				multimedia.ParsingLevel = _level;
				multimedia.XRefID = Database.GenerateXref("OBJE");
											
				record.Multimedia.Add(multimedia.XRefID);
				_ParseState.Records.Push(multimedia);
			}
		}

        // TODO why is parameter not used???
		private string AddSubmitterRecord(GedcomHeader record)
		{
			string xref;
			
			if (_lineValueType == GedcomLineValueType.PointerType)
			{
				xref = _lineValue;
				_missingReferences.Add(xref);

                // TODO there is probably a problem with this... (record may not yet exist?)
			    record.AddSubmitter(Database[xref] as GedcomSubmitterRecord);
			}
			else
			{
				var submitter = new GedcomSubmitterRecord();
				submitter.Level = 0; // always level 0
				submitter.ParsingLevel = _level + 1;
				submitter.XRefID = Database.GenerateXref("S");
				_ParseState.Records.Push(submitter);

				xref = submitter.XRefID;

                record.AddSubmitter(submitter);
			}
			return xref;
		}

		// tag mapping for broken GEDCOM, if it is just
		// a matter of the tag name not being valid we can
		// map to the correct one (or the one we support)
		private string TagMap(string tag)
		{
			string ret = tag;
			switch (tag)
			{
				// we convert _AKA to the admitedly invalid AKA, but we deal
				// with that as a valid tag as it is known to occur in some
				// files.  Ends up adding a name with a type of aka
				case "_AKA":
					ret = "AKA";
					break;
				// we convert _DEG to GRAD, could possibly be EDUC
				case "_DEG":
					_tag = "GRAD";
					break;
				case "_EMAIL":
				case "EMAL": // seen from Generations
					ret = "EMAIL";
					break;
				case "_URL":
				case "URL":
					ret = "WWW";
					break;
			}
			return ret;
		}
		
		#endregion
	}
}

// ReSharper restore UseObjectOrCollectionInitializer
// ReSharper restore SuggestUseVarKeywordEvident
