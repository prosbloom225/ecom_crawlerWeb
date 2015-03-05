<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="control.aspx.cs" Inherits="aspBootstrap.control" MasterPageFile="~/master.Master"%>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div id="wrapper">

            <!-- Page Content -->
            <div id="page-wrapper">
                <div class="row">
                    <div class="col-lg-12">
                        <h1 class="page-header">Crawler Control Panel</h1>
                    </div>
                    <!-- /.col-lg-12 -->
                </div>
                <!-- /.row -->
                <div class="row">
                    <div class="col-lg-6">
                        <div class="panel panel-default">
                            <div class="panel-heading">
                                Crawler Controls
                           
                            </div>
                            <!-- /.panel-heading -->
                            <div class="panel-body">
                                <h4>Crawl Controls</h4>
                                <p>
                                    <asp:Button runat="server" CssClass="btn btn-success btn-lg" OnClick="startCrawl" Text="Start!"></asp:Button>
                                    <asp:Button runat="server" CssClass="btn btn-danger btn-lg" OnClick="stopCrawl" Text="Stop!"></asp:Button>
                                    <asp:Button runat="server" CssClass="btn btn-warning btn-lg" OnClick="pauseCrawl" Text="Pause"></asp:Button>
                                    <asp:Button runat="server" CssClass="btn btn-success btn-lg" OnClick="resumeCrawl" Text="Resume"></asp:Button>
                                </p>
                                <br>
                            </div>
                            <!-- /.panel-body -->
                            <!-- modal Notification -->
                            <div class="modal fade" id="myModal" role="dialog" aria-labelledby="myModalLabel" aria-hidden="true">
                                <div class="modal-dialog">
                                    <asp:UpdatePanel ID="upModal" runat="server" ChildrenAsTriggers="false" UpdateMode="Conditional">
                                        <ContentTemplate>
                                            <div class="modal-content">
                                                <div class="modal-header">
                                                    <button type="button" class="close" data-dismiss="modal" aria-hidden="true">&times;</button>
                                                    <h4 class="modal-title">
                                                        <asp:Label ID="lblModalTitle" runat="server" Text=""></asp:Label></h4>
                                                </div>
                                                <div class="modal-body">
                                                    <asp:Label ID="lblModalBody" runat="server" Text=""></asp:Label>
                                                    <asp:DropDownList ID="ddlModal" runat="server" Visible="false"></asp:DropDownList>
                                                </div>
                                                <div class="modal-footer">
                                                    <asp:Button CssClass="btn btn-info" ID="btnModalAccept" runat="server" Visible="false" Text="Do It!" OnClick="clickCallback" />
                                                    <button class="btn btn-info" data-dismiss="modal" aria-hidden="true">Close</button>
                                                </div>
                                            </div>
                                        </ContentTemplate>
                                    </asp:UpdatePanel>
                                </div>
                            </div>
                            <!-- /notification -->

                            <div class="panel panel-default">
                                <div class="panel-heading">
                                    <h4>Manifest Controls</h4>
                                </div>
                                <div class="panel-body">
                                    <h4>Manifest Controls</h4>
                                    <p>
                                        <asp:Button runat="server" CssClass="btn btn-info btn-lg" Text="Generate Manifest" OnClick="generateManifest"></asp:Button>
                                        <asp:Button CssClass="btn btn-info btn-lg" Text="Add Dept Nums" runat="server" OnClick="addDeptNums"></asp:Button>
                                        <br />
                                    </p>
                                </div>
                                <!-- /.panel-body-->
                            </div>
                            <!-- /.pane-->
                        </div>
                    </div>
                </div>
                <!-- /.row -->
            </div>
            <!-- /#page-wrapper -->

        </div>
        <!-- /#wrapper -->
    </asp:Content> 