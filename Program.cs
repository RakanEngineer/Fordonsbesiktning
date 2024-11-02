using System;
using System.Linq;
using System.Threading;
using static System.Console;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Fordonsbesiktning
{
    class Program
    {
        static string connectionString = "Server=(local);Database=Fordonsbesiktning;Trusted_Connection=True";
        private static ConsoleKeyInfo keyPressed;
        static List<Reservation> reservationList = new List<Reservation>();
        static List<Inspection> inspectionList = new List<Inspection>();
        static void Main(string[] args)
        {
            bool shouldExit = false;
            while (!shouldExit)
            {
                WriteLine("");
                WriteLine("1. Ny reservation");
                WriteLine("2. Lista reservationer");
                WriteLine("3. Utför besiktning");
                WriteLine("4. Lista besiktningar");
                WriteLine("5. Avsluta");

                ConsoleKeyInfo keyPressed = ReadKey(true);
                Clear();
                switch (keyPressed.Key)
                {
                    // Ny reservation
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        NyReservation();
                        break;
                    // Lista reservationer
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        ListaReservationer();
                        ReadKey(true);
                        break;
                    // Utför besiktning
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        UtförBesiktning();
                        break;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        ListaBesiktningar();
                        ReadKey(true);
                        break;
                    case ConsoleKey.D5:
                    case ConsoleKey.NumPad5:
                        shouldExit = true;
                        break;
                }

                Clear();
            }
        }

        private static void ListaBesiktningar()
        {
            List<Inspection> inspectionList = FetchAllBesiktninger();
            WriteLine("");
            WriteLine("Fordon           Utfört datum              Resultat");
            WriteLine("---------------------------------------------------------");
            foreach (Inspection inspection in inspectionList)
            {
                string isApproved = "";
                if (inspection.IsApproved)
                {
                    isApproved = "Godkänd";
                }
                else
                {
                    isApproved = "Ej godkänd";
                }
                WriteLine($"{inspection.RegistrationNumber}        {inspection.PerformedAt.ToString()}          {isApproved}");
            }
            WriteLine("....");
            inspectionList.Clear();
        }

        private static List<Inspection> FetchAllBesiktninger()
        {
            Inspection inspection = null;
            inspectionList.Clear();
            string cmdText = @" SELECT * FROM Inspection ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(cmdText, connection))
            {
                connection.Open();
                SqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    int id = int.Parse(dataReader["Id"].ToString());
                    string registrationNumber = dataReader["RegistrationNumber"].ToString();
                    DateTime PerformedAt = new DateTime();
                    if (dataReader["PerformedAt"].ToString() != "")
                        PerformedAt = DateTime.Parse(dataReader["PerformedAt"].ToString());
                    bool IsApproved = false;
                    if (dataReader["IsApproved"].ToString() != "")
                        IsApproved = bool.Parse(dataReader["IsApproved"].ToString());

                    inspection = new Inspection(id, registrationNumber, PerformedAt, IsApproved);
                    inspectionList.Add(inspection);
                }
                connection.Close();
            }
            //return reservation;
            return inspectionList;
        }

        private static void UtförBesiktning()
        {
            bool isValidInput = false;
            do
            {
                Clear();
                WriteLine("");
                Write("Registreringsnummer: ");
                string registrationNumber = ReadLine();

                Write("Fordonet godkänt? (J)a eller(N)ej");
                bool validKeyPressed;
                do
                {
                    keyPressed = ReadKey(true);
                    validKeyPressed = keyPressed.Key == ConsoleKey.J ||
                                      keyPressed.Key == ConsoleKey.N;
                } while (!validKeyPressed);

                Inspection inspection = Find(registrationNumber);
                //inspection = new Inspection(registrationNumber);
                if (inspection != null)
                {
                    if (keyPressed.Key == ConsoleKey.J)
                    {
                        //godkänt
                        inspectionList.Add(inspection);
                        inspection.Approve();
                        Clear();
                        UpdateInspection(inspection);
                        //WriteLine("information om fordon (");
                        //Thread.Sleep(2000);
                        //isValidInput = true;

                    }
                    if (keyPressed.Key == ConsoleKey.N)
                    {
                        inspectionList.Add(inspection);
                        inspection.Failed();
                        Clear();
                        UpdateInspection(inspection);
                    }
                }
                else
                {
                    Clear();
                    WriteLine("Reservation saknas");
                    Thread.Sleep(2000);
                }
                isValidInput = true;
            } while (!isValidInput);

        }

        private static void UpdateInspection(Inspection inspection)
        {
            string cmdText = @"
                            INSERT INTO Inspection (RegistrationNumber, PerformedAt, IsApproved)
                            VALUES (@RegistrationNumber, @PerformedAt, @IsApproved)
                        ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(cmdText, connection))
            {
                string isApproved = "";
                if (inspection.IsApproved)
                    isApproved = "1";
                else
                    isApproved = "0";
                command.Parameters.AddWithValue("RegistrationNumber", inspection.RegistrationNumber);
                command.Parameters.AddWithValue("PerformedAt", inspection.PerformedAt);
                command.Parameters.AddWithValue("IsApproved", isApproved);

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    //Console.WriteLine("Reservation utförd");
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Error Generated. Details: " + e.ToString());
                }
                finally
                {
                    connection.Close();
                }
                //string cmdText = "UPDATE Reservation SET PerformedAt = @performedAt, IsApproved = @IsApproved WHERE Id = @id ";
                //using (SqlConnection connection = new SqlConnection(connectionString))

                //using (SqlCommand command = new SqlCommand(cmdText, connection))
                //{
                //    string s = "";
                //    if (inspection.IsApproved)
                //        s = "1";
                //    else
                //        s = "0";
                //    command.Parameters.AddWithValue("@id", inspection.Id);
                //    command.Parameters.AddWithValue("@performedAt", inspection.PerformedAt);
                //    command.Parameters.AddWithValue("@IsApproved", s);

                //    try
                //    {
                //        connection.Open();
                //        command.ExecuteNonQuery();
                //    }
                //    catch (SqlException e)
                //    {
                //        Console.WriteLine("Error Generated. Details: " + e.ToString());
                //    }
                //    finally
                //    {
                //        connection.Close();
                //    }
            }


        }

        private static Inspection Find(string registrationNumber)
        {
            Inspection inspection = null;
            string cmdText = @" SELECT *                             
                                FROM Reservation 
                                where RegistrationNumber=@RegistrationNumber
                        ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(cmdText, connection))
            {
                command.Parameters.AddWithValue("@RegistrationNumber", registrationNumber);
                connection.Open();
                SqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    // Person
                    int id = int.Parse(dataReader["Id"].ToString());
                    registrationNumber = dataReader["RegistrationNumber"].ToString();
                    //DateTime performedAt = new DateTime();
                    //if (dataReader["performedAt"].ToString() != "")
                    //    performedAt = DateTime.Parse(dataReader["performedAt"].ToString());
                    inspection = new Inspection(registrationNumber);
                    bool IsApproved;
                    if (Boolean.TryParse(dataReader["IsApproved"].ToString(), out IsApproved))
                        IsApproved = false;
                    else
                        IsApproved = true;
                    inspection = new Inspection(id, registrationNumber, inspection.PerformedAt, IsApproved);
                    inspectionList.Add(inspection);
                }
                connection.Close();
            }
            //return reservation;
            return inspection;
        }

        private static void ListaReservationer()
        {
            List<Reservation> reservationList = FetchAll();
            WriteLine("");
            WriteLine("Fordon                     Datum");
            WriteLine("----------------------------------");
            foreach (Reservation reservation in reservationList)
            {
                WriteLine($"{reservation.RegistrationNumber}                {reservation.Date.ToString()}");
            }
            WriteLine("....");
            reservationList.Clear();

        }

        private static List<Reservation> FetchAll()
        {
            Reservation reservation = null;
            reservationList.Clear();
            string cmdText = @" SELECT Id,            	                      
            	                RegistrationNumber,
            	                Date                                      
                                FROM Reservation                                
                        ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(cmdText, connection))
            {
                connection.Open();
                SqlDataReader dataReader = command.ExecuteReader();
                while (dataReader.Read())
                {
                    int id = int.Parse(dataReader["Id"].ToString());
                    string registrationNumber = dataReader["RegistrationNumber"].ToString();
                    DateTime date = DateTime.Parse(dataReader["Date"].ToString());
                    reservation = new Reservation(id, registrationNumber, date);
                    reservationList.Add(reservation);
                }
                connection.Close();
            }
            //return reservation;
            return reservationList;
        }

        private static void NyReservation()
        {
            // Reservation
            bool isValidInput = false;
            do
            {
                Clear();
                WriteLine("");
                Write("Registreringsnummer: ");
                string registrationNumber = ReadLine();
                Write("Datum (yyyy-MM-dd hh:mm): ");
                DateTime date = DateTime.Parse(ReadLine());
                Write("Är detta korrekt ? (J)a eller(N)ej");
                bool validKeyPressed;
                do
                {
                    keyPressed = ReadKey(true);
                    validKeyPressed = keyPressed.Key == ConsoleKey.J ||
                                      keyPressed.Key == ConsoleKey.N;
                } while (!validKeyPressed);

                if (keyPressed.Key == ConsoleKey.J)
                {
                    Reservation reservation = new Reservation(registrationNumber, date);
                    reservationList.Add(reservation);
                    Clear();
                    SaveReservation(reservation);
                    WriteLine("Reservation utförd");
                    Thread.Sleep(2000);
                    isValidInput = true;
                }
            } while (!isValidInput);


        }

        private static void SaveReservation(Reservation reservation)
        {
            string cmdText = @"
                            INSERT INTO Reservation (RegistrationNumber, Date)
                            VALUES (@RegistrationNumber, @Date)
                        ";
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(cmdText, connection))
            {
                command.Parameters.AddWithValue("RegistrationNumber", reservation.RegistrationNumber);
                command.Parameters.AddWithValue("Date", reservation.Date);
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    //Console.WriteLine("Reservation utförd");
                }
                catch (SqlException e)
                {
                    Console.WriteLine("Error Generated. Details: " + e.ToString());
                }
                finally
                {
                    connection.Close();
                }
            }
        }

    }


}